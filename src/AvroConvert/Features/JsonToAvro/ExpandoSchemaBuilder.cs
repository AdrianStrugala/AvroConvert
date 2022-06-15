using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schema;
using SolTechnology.Avro.AvroObjectServices.Schema.Abstract;

namespace SolTechnology.Avro.Features.JsonToAvro
{
    public class ExpandoSchemaBuilder
    {

        internal TypeSchema BuildSchema(ExpandoObject expandoObject)
        {
            var reflectionSchemaBuilder = new ReflectionSchemaBuilder();

            RecordSchema record = new RecordSchema(new NamedEntityAttributes(
                new SchemaName("UnknownObject"),
                new List<string>(),
                ""),
                typeof(object));


            for (int i = 0; i < expandoObject.Count(); i++)
            {
                var property = expandoObject.ElementAt(i);

                TypeSchema fieldSchema = reflectionSchemaBuilder.BuildSchema(property.Value.GetType());

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
    }
}
