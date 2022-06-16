using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schema.Abstract;
using SolTechnology.Avro;
using System;
using System.Collections.Generic;
using System.IO;

using System.Threading.Tasks;
using Xunit;
using SolTechnology.Avro.AvroObjectServices.Schema;
using SolTechnology.Avro.Features.Serialize;

namespace AvroConvertUnitTests
{
    public class DeserializeUnionOfRecordsTests
    {
        [Fact]
        public void GivenSchemaWithUnionOfRecords_WhenDeserializingWithEachRecord_ThenShouldWork()
        {
            // Scenario with multiple message types in same avro schema with unions:
            // https://www.confluent.io/blog/multiple-event-types-in-the-same-kafka-topic/#avro-unions-with-schema-references

            //Arrange
            var person = new Person { Name = "My Name" };
            var car = new Car { VIN = "My VIN" };
            
            var personSchema = Schema.Create(person);
            var carSchema = Schema.Create(car);

            var unionSchema = new UnionSchema(new List<TypeSchema> { personSchema, carSchema }, typeof(object));

            //Act

            // Serialize with union schema
            byte[] result;
            using (MemoryStream resultStream = new MemoryStream())
            {
                using (var writer = new Encoder(unionSchema, resultStream, CodecType.Null))
                {
                    writer.Append(person);
                }
                result = resultStream.ToArray();
            }

            var avro2Json = AvroConvert.Avro2Json(result, unionSchema.ToString());
            var deserialized = JsonConvert.DeserializeObject<Person>(avro2Json);

            Assert.Equal(person.Name, deserialized.Name);           
        }

        public class Person
        {
            public string Name { get; set; }
        }

        public class Car
        {
            public string VIN { get; set; }
        }
    }
}
