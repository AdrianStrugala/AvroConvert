using AutoFixture;
using AwesomeAssertions;
using Newtonsoft.Json;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.Json2AvroOnly
{
    public class DynamicJson2AvroTests
    {
        private readonly Fixture _fixture = new();

        [Test]
        public void JsonToAvro_ClassWithDictionary_ThrowsNotSupportedException()
        {
            //Arrange
            var testClass = _fixture.Create<ExtendedBaseTestClass>();

            var serializedJson = JsonConvert.SerializeObject(testClass);


            //Act
            var resultAvro = AvroConvert.Json2Avro(serializedJson);


            //Assert
            resultAvro.Should().NotBeNull();
            var exception = Recorded.Exception(() => AvroConvert.Deserialize<ExtendedBaseTestClass>(resultAvro));
            exception.Message.Should().Contain("Unable to deserialize [UnknownObject] of schema [Record] to the target type [AvroConvertComponentTests.ExtendedBaseTestClass].");
            exception.InnerException.Message.Should().Contain("Unable to deserialize [AvroMap] of schema [Record] to the target type [System.Collections.Generic.Dictionary`2[System.String,System.Int32]]");
        }
    }
}
