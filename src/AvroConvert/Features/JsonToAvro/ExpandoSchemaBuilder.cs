using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schema;
using SolTechnology.Avro.AvroObjectServices.Schema.Abstract;
using SolTechnology.Avro.Infrastructure.Extensions;
using SolTechnology.Avro.Infrastructure.Extensions.JsonConvert;

namespace SolTechnology.Avro.Features.JsonToAvro
{
    public class ExpandoSchemaBuilder
    {
        private readonly ReflectionSchemaBuilder _reflectionSchemaBuilder;

        public ExpandoSchemaBuilder()
        {
            _reflectionSchemaBuilder = new ReflectionSchemaBuilder();
        }

        internal TypeSchema BuildSchema(ExpandoObject expandoObject, string name = null)
        {
            RecordSchema record = new RecordSchema(new NamedEntityAttributes(
                new SchemaName(name ?? "UnknownObject"),
                new List<string>(),
                ""),
                typeof(object));


            for (int i = 0; i < expandoObject.Count(); i++)
            {
                var property = expandoObject.ElementAt(i);

                TypeSchema fieldSchema = BuildSchemaInternal(property.Value, property.Key);

                var recordField = new RecordField(
                    new NamedEntityAttributes(new SchemaName(property.Key), new List<string>(), string.Empty),
                    fieldSchema,
                    SortOrder.Ascending,
                    false,
                    null,
                    null,
                    i);

                record.AddField(recordField);
            }


            return record;
        }

        internal TypeSchema BuildSchemaInternal(object item, string name = null)
        {
            TypeSchema fieldSchema;

            if (item is JObject objectProperty)
            {

             //   if (IsDictionary(objectProperty, name))
                // {
                //     throw new NotSupportedException(
                //         $"Property [{name}] recognized as Dictionary. Dictionaries are not supported for anonymous Json2Avro invocation. To resolve the problem, please invoke generic Json2Avro<T> method.");
                // }
                // else
                // {
                    var innerExpandoObject = JsonConvertExtensions.DeserializeExpando<ExpandoObject>(objectProperty.ToString());
                    fieldSchema = BuildSchema(innerExpandoObject, name);
        //        }
            }
            else if (item is JArray arrayProperty)
            {
                fieldSchema = BuildArraySchema(arrayProperty, name);
            }
            else if (item is JValue jValue)
            {
                fieldSchema = _reflectionSchemaBuilder.BuildSchema(jValue.Value?.GetType());
            }
            else
            {
                fieldSchema = _reflectionSchemaBuilder.BuildSchema(item.GetType());
            }

            return fieldSchema;
        }


        internal TypeSchema BuildArraySchema(JArray incomingObject, string name = null)
        {
            var xd = incomingObject.FirstOrDefault();

            TypeSchema childSchema = BuildSchemaInternal(xd, name);

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


        private bool IsDictionary(JObject objectProperty, string name)
        {
            //No idea how to do this better

            if (objectProperty.HasValues)
            {
                if (objectProperty.First != null && objectProperty.First<object>().GetType() != typeof(string))
                {
                    return true;
                }

                if (name.Contains("Dictionary") ||
                    name.Contains("dictionary") ||
                    name.Contains("dict") ||
                    name.Contains("Dict") ||
                    name.Contains("Map") ||
                    name.Contains("map"))
                {
                    return true;
                }

                return false;
            }

            return false;
        }

    }
}
