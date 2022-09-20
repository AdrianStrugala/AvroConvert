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


            // if (IsDictionary(jObject))
            // {
            //     return BuildDictionarySchema(jObject, name);
            // }

            //   if (IsDictionary(objectProperty, name))
            // {
            //     throw new NotSupportedException(
            //         $"Property [{name}] recognized as Dictionary. Dictionaries are not supported for anonymous Json2Avro invocation. To resolve the problem, please invoke generic Json2Avro<T> method.");
            // }
            // else
            // {
            // var innerExpandoObject = JsonConvertExtensions.DeserializeExpando<ExpandoObject>(objectProperty.ToString());

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
                                        SortOrder.Ascending,
                                        false,
                                        null,
                                        null,
                                        i);
                }
                catch (SerializationException serializationException)
                {
                    warning = $"{warning} [{serializationException.Message}]";
                    recordFieldSchema = new RecordFieldSchema(
                                     new NamedEntityAttributes(new SchemaName(property.Name, true), new List<string> { property.Name }, warning),
                                     fieldSchema,
                                     SortOrder.Ascending,
                                     false,
                                     null,
                                     null,
                                     i);
                }


                record.AddField(recordFieldSchema);
            }


            return record;
        }

   


        internal TypeSchema BuildArraySchema(JArray incomingObject, string name = null)
        {
            var xd = incomingObject.FirstOrDefault();

            TypeSchema childSchema = BuildSchema(xd, name);

            // var enumerable = incomingObject.GetType().FindEnumerableType();
            // if (enumerable != null)
            // {
            //     var childItem = ((IList)incomingObject)[0];
            //
            //     var childExpando = JsonConvertExtensions.DeserializeExpando<ExpandoObject>(xd?.ToString());
            //
            //   
            //     childSchema = BuildSchema(childExpando, name);
            // }

            ArraySchema array = new ArraySchema(childSchema, typeof(object));

            return array;
        }


        private bool IsDictionary(JObject objectProperty)
        {
            //No idea how to do this better
            if (objectProperty.HasValues)
            {
                // objectProperty.First.

                if (objectProperty.First != null && objectProperty.First<object>().GetType() != typeof(string))
                {
                    return true;
                }

                return false;
            }

            return false;
        }


        private TypeSchema BuildDictionarySchema(JObject jObject, string name)
        {
            var x = jObject.First;

            throw new System.NotImplementedException();
        }
    }
}
