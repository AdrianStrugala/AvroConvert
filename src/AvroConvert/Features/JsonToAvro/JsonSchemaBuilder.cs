using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Attributes;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.Features.JsonToAvro
{
    internal class JsonSchemaBuilder
    {
        private readonly ReflectionSchemaBuilder _reflectionSchemaBuilder;

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal JsonSchemaBuilder()
        {
            _reflectionSchemaBuilder = new ReflectionSchemaBuilder();
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal TypeSchema BuildSchema(object item, string name = null)
        {
            if (item == null)
            {
                return Schema.Create((object)null);
            }

            TypeSchema fieldSchema = item switch
            {
                JObject objectProperty => BuildRecordSchema(objectProperty.Properties().Select(p => new KeyValuePair<string, object>(p.Name, p.Value)), name),
                JArray arrayProperty => BuildArraySchema(arrayProperty, name),
                JValue jValue => _reflectionSchemaBuilder.BuildSchema(jValue.Value?.GetType()),
                ExpandoObject expando => BuildSchema(new[] { expando }, name),
                IEnumerable<ExpandoObject> expandos => new ArraySchema(BuildSchema(expandos, name), typeof(object)),
                // element type `object` says nothing; take the shape from the first non-null element like JArray
                IEnumerable<object> objects when item is not string && item is not IDictionary
                    => new ArraySchema(BuildSchema(objects.FirstOrDefault(x => x != null), name), typeof(object)),
                _ => _reflectionSchemaBuilder.BuildSchema(item.GetType())
            };

            return fieldSchema;
        }

        /// <summary>Record schema covering every item: a field per key seen, nullable when any item lacks it or holds null.</summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal TypeSchema BuildSchema(IEnumerable<ExpandoObject> items, string name = null)
        {
            return BuildMergedRecordSchema(items.Select(item => (IDictionary<string, object>)item), name, typeof(ExpandoObject));
        }

        /// <summary>
        /// Schema of the elements of a JSON array. Objects are merged like <see cref="BuildSchema(IEnumerable{ExpandoObject}, string)"/>,
        /// so a field that is missing or null in any element becomes a union with null; other elements take the shape of the first one.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal TypeSchema BuildItemSchema(JArray array, string name = null)
        {
            var items = array.Where(t => t.Type != JTokenType.Null).ToList();
            if (items.Count == 0 || !items.All(t => t is JObject))
            {
                return BuildSchema(array.FirstOrDefault(), name);
            }

            var record = BuildMergedRecordSchema(
                items.Cast<JObject>().Select(o => (IDictionary<string, object>)o.Properties().ToDictionary(
                    p => p.Name,
                    p => p.Value.Type == JTokenType.Null ? null : (object)p.Value)),
                name,
                typeof(JObject));

            return items.Count < array.Count
                ? new UnionSchema(new List<TypeSchema> { new NullSchema(), record }, typeof(object))
                : record;
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private TypeSchema BuildMergedRecordSchema(IEnumerable<IDictionary<string, object>> items, string name, System.Type runtimeType)
        {
            var seen = new Dictionary<string, (TypeSchema Schema, bool Nullable)>();
            var order = new List<string>();
            int count = 0;
            foreach (var item in items)
            {
                count++;
                foreach (var kv in item)
                {
                    if (!seen.TryGetValue(kv.Key, out var entry))
                    {
                        order.Add(kv.Key);
                        entry = (null, count > 1);
                    }

                    if (kv.Value == null)
                    {
                        entry.Nullable = true;
                    }
                    else if (entry.Schema == null || (entry.Schema is IntSchema or LongSchema && IsFloatingPoint(kv.Value)))
                    {
                        // A whole number first and a fraction later is a double, not a long that would truncate it.
                        entry.Schema = BuildSchema(kv.Value, kv.Key);
                    }

                    seen[kv.Key] = entry;
                }

                foreach (var key in order)
                {
                    if (!item.ContainsKey(key))
                    {
                        seen[key] = (seen[key].Schema, true);
                    }
                }
            }

            var result = new RecordSchema(new NamedEntityAttributes(new SchemaName(name ?? "UnknownObject"), new List<string>(), ""), runtimeType);
            int position = 0;
            foreach (var key in order)
            {
                var (schema, nullable) = seen[key];
                TypeSchema fieldSchema = schema == null ? new NullSchema()
                    : nullable && schema is not UnionSchema && schema is not NullSchema ? new UnionSchema(new List<TypeSchema> { new NullSchema(), schema }, typeof(object))
                    : schema;
                result.AddField(CreateField(key, fieldSchema, position++));
            }

            return result;
        }

        private static bool IsFloatingPoint(object value) =>
            value is double or float or decimal || value is JValue { Type: JTokenType.Float };

        /// <summary>A field named after a JSON key; a key that is not a valid Avro name gets a valid one and keeps the key as alias.</summary>
        private static RecordFieldSchema CreateField(string key, TypeSchema schema, int position)
        {
            try
            {
                return new RecordFieldSchema(new NamedEntityAttributes(new SchemaName(key), new List<string>(), string.Empty), schema, false, null, position);
            }
            catch (SerializationException serializationException)
            {
                return new RecordFieldSchema(
                    new NamedEntityAttributes(new SchemaName(key, true), new List<string> { key }, $" [{serializationException.Message}]"),
                    schema,
                    false,
                    null,
                    position);
            }
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal TypeSchema BuildRecordSchema(IEnumerable<KeyValuePair<string, object>> properties, string name = null)
        {
            RecordSchema record = new RecordSchema(new NamedEntityAttributes(
                new SchemaName(name ?? "UnknownObject"),
                new List<string>(),
                ""),
                typeof(JObject));


            //TODO: In one day Dictionaries will be handled: it's here

            int i = 0;
            foreach (var property in properties)
            {
                TypeSchema fieldSchema = BuildSchema(property.Value, property.Key);

                string warning = string.Empty;
                RecordFieldSchema recordFieldSchema = null;

                try
                {
                    recordFieldSchema = new RecordFieldSchema(
                                        new NamedEntityAttributes(new SchemaName(property.Key), new List<string>(), warning),
                                        fieldSchema,
                                        false,
                                        null,
                                        i);
                }
                catch (SerializationException serializationException)
                {
                    warning = $"{warning} [{serializationException.Message}]";
                    recordFieldSchema = new RecordFieldSchema(
                                     new NamedEntityAttributes(new SchemaName(property.Key, true), new List<string> { property.Key }, warning),
                                     fieldSchema,
                                     false,
                                     null,
                                     i);
                }


                record.AddField(recordFieldSchema);
                i++;
            }


            return record;
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal TypeSchema BuildArraySchema(JArray incomingObject, string name = null)
        {
            return new ArraySchema(BuildItemSchema(incomingObject, name), typeof(object));
        }
    }
}
