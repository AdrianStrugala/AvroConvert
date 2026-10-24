using AwesomeAssertions;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro;
using System;
using System.IO;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using NUnit.Framework;
using SolTechnology.Avro.Features.Serialize;

namespace AvroConvertUnitTests
{
    public class DeserializeUnionTests
    {
        [TestCase(typeof(Person))]
        [TestCase(typeof(DateTimeOffset))]
        [TestCase(typeof(DateTime))]
        [TestCase(typeof(Guid))]
        [TestCase(typeof(string))]
        [TestCase(typeof(int))]
        [TestCase(typeof(float))]
        [TestCase(typeof(long))]
        [TestCase(typeof(double))]
        [TestCase(typeof(bool))]
        [TestCase(typeof(byte[]))]
        public void UnionsOfSameTypeShouldThrowException(Type type)
        {
            var unionSchema = new UnionSchema(Schema.Create(type), Schema.Create(type));

            FluentActions.Invoking(() => 
                Schema.Parse(unionSchema.ToString())).Should().Throw<Exception>();
        }

        [Test]
        public void UnionsOfDifferentRecordTypesShouldBeAllowed()
        {
            var unionSchema = new UnionSchema(Schema.Create(typeof(Person)), Schema.Create(typeof(Car)));

            var fromJsonSchema = Schema.Parse(unionSchema.ToString());

            fromJsonSchema.Should().NotBeNull();
        }

        [Test]
        public void GivenSchemaWithUnionOfRecords_WhenDeserializingFirstTypeOfUnion_ThenShouldWork()
        {
            // Scenario with multiple message types in same avro schema with unions:
            // https://www.confluent.io/blog/multiple-event-types-in-the-same-kafka-topic/#avro-unions-with-schema-references

            //Arrange
            var person = new Person { Name = "My Name" };

            // NOTE: Person is first schema in union
            var unionSchema = new UnionSchema(Schema.Create(typeof(Person)), Schema.Create(typeof(Car)));

            //Act
            // Serialize/deserialize with union schema
            var json = ToJsonViaAvro(person, unionSchema);
            var deserialized = JsonConvert.DeserializeObject<Person>(json);

            deserialized.Name.Should().Be(person.Name);
        }

        [Test]
        public void GivenSchemaWithUnionOfRecords_WhenDeserializingSecondTypeOfUnion_ThenShouldWork()
        {
            // Scenario with multiple message types in same avro schema with unions:
            // https://www.confluent.io/blog/multiple-event-types-in-the-same-kafka-topic/#avro-unions-with-schema-references

            //Arrange
            var person = new Person { Name = "My Name" };

            // NOTE: Person is second schema in union
            var unionSchema = new UnionSchema(Schema.Create(typeof(Car)), Schema.Create(typeof(Person)));

            //Act
            // Serialize/deserialize with union schema
            var json = ToJsonViaAvro(person, unionSchema);
            var deserialized = JsonConvert.DeserializeObject<Person>(json);

            deserialized.Name.Should().Be(person.Name);
        }

        private string ToJsonViaAvro<T>(T data, TypeSchema schema)
        {
            byte[] result;
            using (MemoryStream resultStream = new MemoryStream())
            {
                using (var writer = new Encoder(schema, resultStream, CodecType.Null))
                {
                    writer.Append(data);
                }
                result = resultStream.ToArray();
            }

            var avro2Json = AvroConvert.Avro2Json(result, schema.ToString());
            return avro2Json;
        }        
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
