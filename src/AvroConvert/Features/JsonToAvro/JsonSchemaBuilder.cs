using System.Collections.Generic;
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
                JObject objectProperty => BuildRecordSchema(objectProperty, name),
                JArray arrayProperty => BuildArraySchema(arrayProperty, name),
                JValue jValue => _reflectionSchemaBuilder.BuildSchema(jValue.Value?.GetType()),
                _ => _reflectionSchemaBuilder.BuildSchema(item.GetType())
            };

            return fieldSchema;
        }

        internal TypeSchema BuildRecordSchema(JObject jObject, string name = null)
        {
            RecordSchema record = new RecordSchema(new NamedEntityAttributes(
                new SchemaName(name ?? "UnknownObject"),
                new List<string>(),
                ""),
                typeof(JObject));


            //TODO: In one day Dictionaries will be handled: it's here

            for (int i = 0; i < jObject.Properties().Count(); i++)
            {
                var property = jObject.Properties().ElementAt(i);

                TypeSchema fieldSchema = BuildSchema(property.Value, property.Name);

                string warning = string.Empty;
                RecordFieldSchema recordFieldSchema = null;

                try
                {
                    recordFieldSchema = new RecordFieldSchema(
                                        new NamedEntityAttributes(new SchemaName(property.Name), new List<string>(), warning),
                                        fieldSchema,
                                        false,
                                        null,
                                        i);
                }
                catch (SerializationException serializationException)
                {
                    warning = $"{warning} [{serializationException.Message}]";
                    recordFieldSchema = new RecordFieldSchema(
                                     new NamedEntityAttributes(new SchemaName(property.Name, true), new List<string> { property.Name }, warning),
                                     fieldSchema,
                                     false,
                                     null,
                                     i);
                }


                record.AddField(recordFieldSchema);
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
