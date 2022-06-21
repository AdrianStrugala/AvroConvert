using System;
using System.Collections.Generic;
using AutoFixture;
using FluentAssertions;
using Newtonsoft.Json;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.JsonToAvro
{
    public class JsonToAvroTests
    {
        private readonly Fixture _fixture;

        public JsonToAvroTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void JsonToAvro_SampleClass_ProducedDesiredAvro()
        {
            //Arrange
            var user = new User();
            user.favorite_color = "blue";
            user.favorite_number = 2137;
            user.name = "red";

            var serializedJson = JsonConvert.SerializeObject(user);


            //Act
            var resultAvro = AvroConvert.Json2Avro(serializedJson);


            //Assert
            var deserialized = AvroConvert.Deserialize<User>(resultAvro);

            deserialized.Should().BeEquivalentTo(user);
        }

        [Fact]
        public void JsonToAvro_ComplexClass_ProducedDesiredAvro()
        {
            //Arrange
            var testClass = _fixture.Create<VeryComplexClass>();

            var serializedJson = JsonConvert.SerializeObject(testClass);


            //Act
            var resultAvro = AvroConvert.Json2Avro(serializedJson);


            //Assert
            var deserialized = AvroConvert.Deserialize<VeryComplexClass>(resultAvro);
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Fact]
        public void JsonToAvro_String_ProducedDesiredAvro()
        {
            //Arrange
            var testClass = _fixture.Create<string>();

            var serializedJson = JsonConvert.SerializeObject(testClass);


            //Act
            var resultAvro = AvroConvert.Json2Avro(serializedJson);


            //Assert
            var deserialized = AvroConvert.Deserialize<string>(resultAvro);
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Fact]
        public void JsonToAvro_Int_ProducedDesiredAvro()
        {
            //Arrange
            var testClass = _fixture.Create<int>();

            var serializedJson = JsonConvert.SerializeObject(testClass);


            //Act
            var resultAvro = AvroConvert.Json2Avro(serializedJson);


            //Assert
            var deserialized = AvroConvert.Deserialize<int>(resultAvro);
            deserialized.Should().Be(testClass);
        }

        [Fact]
        public void JsonToAvro_Long_ProducedDesiredAvro()
        {
            //Arrange
            var testClass = _fixture.Create<long>();

            var serializedJson = JsonConvert.SerializeObject(testClass);


            //Act
            var resultAvro = AvroConvert.Json2Avro(serializedJson);


            //Assert
            var deserialized = AvroConvert.Deserialize<long>(resultAvro);
            deserialized.Should().Be(testClass);
        }

        [Fact]
        public void JsonToAvro_Double_ProducedDesiredAvro()
        {
            //Arrange
            var testClass = _fixture.Create<double>();

            var serializedJson = JsonConvert.SerializeObject(testClass);


            //Act
            var resultAvro = AvroConvert.Json2Avro(serializedJson);


            //Assert
            var deserialized = AvroConvert.Deserialize<double>(resultAvro);
            deserialized.Should().Be(testClass);
        }

        [Fact]
        public void JsonToAvro_Float_ProducedDesiredAvro()
        {
            //Arrange
            var testClass = _fixture.Create<float>();

            var serializedJson = JsonConvert.SerializeObject(testClass);


            //Act
            var resultAvro = AvroConvert.Json2Avro(serializedJson);


            //Assert
            var deserialized = AvroConvert.Deserialize<float>(resultAvro);
            deserialized.Should().Be(testClass);
        }

        [Fact]
        public void JsonToAvro_Decimal_ProducedDesiredAvro()
        {
            //Arrange
            var testClass = _fixture.Create<decimal>();

            var serializedJson = JsonConvert.SerializeObject(testClass);


            //Act
            var resultAvro = AvroConvert.Json2Avro(serializedJson);


            //Assert
            var deserialized = AvroConvert.Deserialize<decimal>(resultAvro);
            deserialized.Should().Be(testClass);
        }

        [Fact]
        public void JsonToAvro_ListOfClass_ProducedDesiredAvro()
        {
            //Arrange
            var testClass = _fixture.CreateMany<User>();

            var serializedJson = JsonConvert.SerializeObject(testClass);


            //Act
            var resultAvro = AvroConvert.Json2Avro(serializedJson);


            //Assert
            var deserialized = AvroConvert.Deserialize<List<User>>(resultAvro);
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Fact]
        public void JsonToAvro_ClassWithDictionary_ThrowsNotSupportedException()
        {
            //Arrange
            var testClass = _fixture.Create<ExtendedBaseTestClass>();

            var serializedJson = JsonConvert.SerializeObject(testClass);


            //Act
            var exception = Record.Exception(() => AvroConvert.Json2Avro(serializedJson));


            //Assert
            exception.Should().BeOfType<NotSupportedException>();
        }
    }
}
