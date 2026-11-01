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
        private readonly Dictionary<string, (string Shape, RecordSchema Record)> _records = new();

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
                // element type `object` says nothing; RuntimeSchema completes it from the elements
                IEnumerable<object> when item is not string && item is not IDictionary => _reflectionSchemaBuilder.BuildSchema(typeof(List<object>)),
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
        /// Schema of the elements of a JSON array, whatever the elements hold. Objects are merged like
        /// <see cref="BuildSchema(IEnumerable{ExpandoObject}, string)"/>, so a field that is missing or null in any element becomes a
        /// union with null; arrays take the elements of all of them; different values give a union, so nothing changes its type.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal TypeSchema BuildItemSchema(IEnumerable<JToken> array, string name = null)
        {
            // One value of each type says all there is to say about an array, however long it is.
            var objects = new List<JObject>();
            var arrays = new List<JArray>();
            var values = new Dictionary<JTokenType, JValue>();
            JValue number = null;
            JToken first = null;
            bool hasNull = false;

            foreach (var item in array)
            {
                first ??= item;
                switch (item)
                {
                    case JObject jObject:
                        objects.Add(jObject);
                        break;
                    case JArray jArray:
                        arrays.Add(jArray);
                        break;
                    case JValue { Type: JTokenType.Null }:
                        hasNull = true;
                        break;
                    case JValue { Type: JTokenType.Integer or JTokenType.Float } value:
                        // A whole number next to a fraction is a double, not a long that would truncate it.
                        number = number == null || !IsFloatingPoint(number) && IsFloatingPoint(value) ? value : number;
                        break;
                    case JValue value:
                        values.TryAdd(value.Type, value);
                        break;
                }
            }

            if (objects.Count == 0 && arrays.Count == 0 && values.Count == 0 && number == null)
            {
                return BuildSchema(first, name);
            }

            var branches = new List<TypeSchema>();
            if (hasNull)
            {
                branches.Add(new NullSchema());
            }

            if (objects.Count > 0)
            {
                branches.Add(BuildMergedRecordSchema(
                    objects.Select(o => (IDictionary<string, object>)o.Properties().ToDictionary(
                        p => p.Name,
                        p => p.Value.Type == JTokenType.Null ? null : (object)p.Value)),
                    name,
                    typeof(JObject)));
            }

            if (arrays.Count > 0)
            {
                branches.Add(new ArraySchema(BuildItemSchema(arrays.SelectMany(a => a), name), typeof(object)));
            }

            foreach (var schema in values.Values.Append(number).Where(v => v != null).Select(v => BuildSchema(v, name)).GroupBy(s => s.UnionKey).Select(g => g.First()))
            {
                branches.Add(schema);
            }

            return branches.Count == 1 ? branches[0] : new UnionSchema(branches, typeof(object));
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

            var fields = new List<RecordFieldSchema>();
            foreach (var key in order)
            {
                var (schema, nullable) = seen[key];
                TypeSchema fieldSchema = schema == null ? new NullSchema()
                    : nullable && schema is not UnionSchema && schema is not NullSchema ? new UnionSchema(new List<TypeSchema> { new NullSchema(), schema }, typeof(object))
                    : schema;
                fields.Add(CreateField(key, fieldSchema, fields.Count));
            }

            return NamedRecord(name, fields, runtimeType);
        }

        /// <summary>
        /// A record takes its name from a JSON key, and one key can hold different objects in different places, while a name
        /// can be defined only once. The same fields share one record, other fields get a name of their own.
        /// </summary>
        private RecordSchema NamedRecord(string name, List<RecordFieldSchema> fields, System.Type runtimeType)
        {
            name ??= "UnknownObject";
            var shape = string.Join(",", fields.Select(field => $"{field.Name}:{field.TypeSchema}"));

            for (int i = 1; ; i++)
            {
                var candidate = i == 1 ? name : $"{name}_{i}";
                if (_records.TryGetValue(candidate, out var known))
                {
                    if (known.Shape == shape)
                    {
                        return known.Record;
                    }

                    continue;
                }

                var result = new RecordSchema(new NamedEntityAttributes(new SchemaName(candidate), new List<string>(), ""), runtimeType);
                fields.ForEach(result.AddField);
                _records[candidate] = (shape, result);
                return result;
            }
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
            //TODO: In one day Dictionaries will be handled: it's here

            var fields = new List<RecordFieldSchema>();
            foreach (var property in properties)
            {
                fields.Add(CreateField(property.Key, BuildSchema(property.Value, property.Key), fields.Count));
            }

            return NamedRecord(name, fields, typeof(JObject));
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal TypeSchema BuildArraySchema(JArray incomingObject, string name = null)
        {
            return new ArraySchema(BuildItemSchema(incomingObject, name), typeof(object));
        }
    }
}
