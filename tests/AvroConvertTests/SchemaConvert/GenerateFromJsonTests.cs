using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.Kernel;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace AvroConvertComponentTests.SchemaConvert
{
    public class GenerateFromJsonTests
    {
        private readonly Fixture _fixture;

        public GenerateFromJsonTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Generates_Avro_schema_from_class_serialized_to_json()
        {
            //Arrange
            var underTest = _fixture.Create<BaseTestClass>();
            var json = JsonConvert.SerializeObject(underTest);

            var expected =
                "{\"name\":\"UnknownObject\",\"type\":\"record\",\"fields\":[{\"name\":\"justSomeProperty\",\"type\":\"string\"},{\"name\":\"andLongProperty\",\"type\":\"long\"},{\"name\":\"objectProperty\",\"type\":{\"name\":\"objectProperty\",\"type\":\"record\",\"fields\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"favorite_number\",\"type\":\"long\"},{\"name\":\"favorite_color\",\"type\":\"string\"}]}}]}";


            //Act
            var result = SolTechnology.Avro.SchemaConvert.GenerateFromJson(json);


            //Assert
            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void Generates_Avro_schema_from_array_serialized_to_json()
        {
            //Arrange
            var underTest = _fixture.CreateMany<User>();
            var json = JsonConvert.SerializeObject(underTest);

            var expected =
                "{\"type\":\"array\",\"items\":{\"name\":\"UnknownObject\",\"type\":\"record\",\"fields\":[{\"name\":\"name\",\"type\":\"string\"},{\"name\":\"favorite_number\",\"type\":\"long\"},{\"name\":\"favorite_color\",\"type\":\"string\"}]}}";


            //Act
            var result = SolTechnology.Avro.SchemaConvert.GenerateFromJson(json);


            //Assert
            result.Should().BeEquivalentTo(expected);
        }


        [Theory]
        [InlineData(typeof(int), "\"long\"")]
        [InlineData(typeof(string), "\"string\"")]
        [InlineData(typeof(decimal), "\"double\"")]
        public void Generates_Avro_schema_from_primitives_serialized_to_json(Type primitiveType, string expectedSchema)
        {
            //Arrange
            var underTest = _fixture.Create(primitiveType, new SpecimenContext(_fixture));
            var json = JsonConvert.SerializeObject(underTest);


            //Act
            var result = SolTechnology.Avro.SchemaConvert.GenerateFromJson(json);


            //Assert
            result.Should().BeEquivalentTo(expectedSchema);
        }


        [Fact]
        public void Generates_Avro_schema_from_dictionary_of_non_string_key_is_represented_as_record()
        {
            //Arrange
            var underTest = _fixture.Create<Dictionary<int, string>>();
            var json = JsonConvert.SerializeObject(underTest);

            //Act
            var result = SolTechnology.Avro.SchemaConvert.GenerateFromJson(json);


            //Assert
            result.Should().StartWith("{\"name\":\"UnknownObject\",\"type\":\"record\",\"fields\":");
        }

        [Fact]
        public void Generates_Avro_schema_from_dictionary_of_string_key_is_represented_as_record()
        {
            //Arrange
            var underTest = _fixture.Create<Dictionary<string, string>>();
            var json = JsonConvert.SerializeObject(underTest);


            //Act
            var result = SolTechnology.Avro.SchemaConvert.GenerateFromJson(json);

            //Assert
            result.Should().StartWith("{\"name\":\"UnknownObject\",\"type\":\"record\",\"fields\":");
        }

    }
}
