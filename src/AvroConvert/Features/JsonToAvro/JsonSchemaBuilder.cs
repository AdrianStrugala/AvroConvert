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

namespace SolTechnology.Avro.Features.JsonToAvro
{
    internal class JsonSchemaBuilder
    {
        private readonly ReflectionSchemaBuilder _reflectionSchemaBuilder;

        internal JsonSchemaBuilder()
        {
            _reflectionSchemaBuilder = new ReflectionSchemaBuilder();
        }

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
        internal TypeSchema BuildSchema(IEnumerable<ExpandoObject> items, string name = null)
        {
            var seen = new Dictionary<string, (TypeSchema Schema, bool Nullable)>();
            var order = new List<string>();
            int count = 0;
            foreach (IDictionary<string, object> item in items)
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
                    else
                    {
                        entry.Schema ??= BuildSchema(kv.Value, kv.Key);
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

            var result = new RecordSchema(new NamedEntityAttributes(new SchemaName(name ?? "UnknownObject"), new List<string>(), ""), typeof(ExpandoObject));
            int position = 0;
            foreach (var key in order)
            {
                var (schema, nullable) = seen[key];
                TypeSchema fieldSchema = schema == null ? new NullSchema()
                    : nullable && schema is not UnionSchema && schema is not NullSchema ? new UnionSchema(new List<TypeSchema> { new NullSchema(), schema }, typeof(object))
                    : schema;
                result.AddField(new RecordFieldSchema(new NamedEntityAttributes(new SchemaName(key), new List<string>(), string.Empty), fieldSchema, false, null, position++));
            }

            return result;
        }

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

        internal TypeSchema BuildArraySchema(JArray incomingObject, string name = null)
        {
            var childObject = incomingObject.FirstOrDefault();

            TypeSchema childSchema = BuildSchema(childObject, name);

            ArraySchema array = new ArraySchema(childSchema, typeof(object));

            return array;
        }
    }
}
