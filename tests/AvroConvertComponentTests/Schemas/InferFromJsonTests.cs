using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.Kernel;
using AwesomeAssertions;
using Newtonsoft.Json;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.Schemas
{
    public class InferFromJsonTests
    {
        private readonly Fixture _fixture;

        public InferFromJsonTests()
        {
            _fixture = new Fixture();
        }

        [Test]
        public void Generates_Avro_schema_from_class_serialized_to_json()
        {
            //Arrange
            var underTest = _fixture.Create<BaseTestClass>();
            var json = JsonConvert.SerializeObject(underTest);

            var expected =
                "{\"name\":\"UnknownObject\",\"type\":\"record\",\"fields\":[{\"name\":\"justSomeProperty\",\"type\":\"string\"},{\"name\":\"andLongProperty\",\"type\":\"long\"},{\"name\":\"objectProperty\",\"type\":{\"name\":\"objectProperty\",\"type\":\"record\",\"fields\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"favorite_number\",\"type\":\"long\"},{\"name\":\"favorite_color\",\"type\":\"string\"}]}}]}";


            //Act
            var result = AvroSchema.InferFromJson(json).Json;


            //Assert
            result.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void Generates_Avro_schema_from_array_serialized_to_json()
        {
            //Arrange
            var underTest = _fixture.CreateMany<User>();
            var json = JsonConvert.SerializeObject(underTest);

            var expected =
                "{\"type\":\"array\",\"items\":{\"name\":\"UnknownObject\",\"type\":\"record\",\"fields\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"favorite_number\",\"type\":\"long\"},{\"name\":\"favorite_color\",\"type\":\"string\"}]}}";


            //Act
            var result = AvroSchema.InferFromJson(json).Json;


            //Assert
            result.Should().BeEquivalentTo(expected);
        }


        [TestCase(typeof(int), "\"long\"")]
        [TestCase(typeof(string), "\"string\"")]
        [TestCase(typeof(decimal), "\"double\"")]
        public void Generates_Avro_schema_from_primitives_serialized_to_json(Type primitiveType, string expectedSchema)
        {
            //Arrange
            var underTest = _fixture.Create(primitiveType, new SpecimenContext(_fixture));
            var json = JsonConvert.SerializeObject(underTest);


            //Act
            var result = AvroSchema.InferFromJson(json).Json;


            //Assert
            result.Should().BeEquivalentTo(expectedSchema);
        }


        [Test]
        public void Generates_Avro_schema_from_dictionary_of_non_string_key_is_represented_as_record()
        {
            //Arrange
            var underTest = _fixture.Create<Dictionary<int, string>>();
            var json = JsonConvert.SerializeObject(underTest);

            //Act
            var result = AvroSchema.InferFromJson(json).Json;


            //Assert
            result.Should().StartWith("{\"name\":\"UnknownObject\",\"type\":\"record\",\"fields\":");
        }

        [Test]
        public void Generates_Avro_schema_from_dictionary_of_string_key_is_represented_as_record()
        {
            //Arrange
            var underTest = _fixture.Create<Dictionary<string, string>>();
            var json = JsonConvert.SerializeObject(underTest);


            //Act
            var result = AvroSchema.InferFromJson(json).Json;

            //Assert
            result.Should().StartWith("{\"name\":\"UnknownObject\",\"type\":\"record\",\"fields\":");
        }


        [Test]
        public void Fields_null_or_missing_in_any_array_element_become_nullable()
        {
            //Arrange
            var json = """[{ "id": 1, "note": "a" }, { "id": 2, "note": null }, { "id": 3, "added": 1.5 }]""";


            //Act
            var result = AvroSchema.InferFromJson(json).Json;


            //Assert
            result.Should().Be("{\"type\":\"array\",\"items\":{\"name\":\"UnknownObject\",\"type\":\"record\",\"fields\":[{\"name\":\"id\",\"type\":\"long\"},{\"name\":\"note\",\"type\":[\"null\",\"string\"]},{\"name\":\"added\",\"type\":[\"null\",\"double\"]}]}}");
        }
    }
}
