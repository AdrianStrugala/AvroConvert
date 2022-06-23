using AutoFixture;
using FluentAssertions;
using Newtonsoft.Json;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.JsonToAvro
{
    public class Json2AvroTests
    {
        private readonly Fixture _fixture;

        public Json2AvroTests()
        {
            _fixture = new AutoFixture.Fixture();
        }

        [Fact]
        public void Json2Avro_ConvertUserType_ProducedDesiredAvro()
        {
            //Arrange
            var user = new User();
            user.favorite_color = "blue";
            user.favorite_number = 2137;
            user.name = "red";

            var serializedJson = JsonConvert.SerializeObject(user);


            //Act
            var result = AvroConvert.Json2Avro<User>(serializedJson);

            var deserialized = AvroConvert.Deserialize<User>(result);


            //Assert
            deserialized.Should().BeEquivalentTo(user);
        }

        [Fact]
        public void Json2Avro_ConvertVeryComplexType_ProducedDesiredAvro()
        {
            //Arrange
            var classUnderTest = _fixture.Create<VeryComplexClass>();

            var serializedJson = JsonConvert.SerializeObject(classUnderTest);



            //Act
            var result = AvroConvert.Json2Avro<VeryComplexClass>(serializedJson);

            var deserialized = AvroConvert.Deserialize<VeryComplexClass>(result);

            //Assert
            deserialized.Should().BeEquivalentTo(classUnderTest);
        }

        [Fact]
        public void Json2Avro_ConvertClassWithDictionaryAndEnum_ProducedDesiredAvro()
        {
            //Arrange
            var classUnderTest = _fixture.Create<ExtendedBaseTestClass>();

            var serializedJson = JsonConvert.SerializeObject(classUnderTest);



            //Act
            var result = AvroConvert.Json2Avro<ExtendedBaseTestClass>(serializedJson);

            var deserialized = AvroConvert.Deserialize<ExtendedBaseTestClass>(result);


            //Assert
            deserialized.Should().BeEquivalentTo(classUnderTest);

        }
    }
}
