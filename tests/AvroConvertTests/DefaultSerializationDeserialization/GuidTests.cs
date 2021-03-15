using System;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.DefaultSerializationDeserialization
{
    public class GuidTests
    {
        private readonly Fixture _fixture;

        public GuidTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Component_Guid_ResultIsTheSameAsInput()
        {
            //Arrange
            Guid testClass = _fixture.Create<Guid>();

            //Act
            var result = AvroConvert.Serialize(testClass);
            var deserialized = AvroConvert.Deserialize<Guid>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testClass, deserialized);
        }

        [Fact]
        public void Component_ObjectContainsGuid_ResultIsTheSameAsInput()
        {
            //Arrange
            ClassWithGuid testClass = _fixture.Create<ClassWithGuid>();

            //Act
            var result = AvroConvert.Serialize(testClass);
            var deserialized = AvroConvert.Deserialize<ClassWithGuid>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testClass.theGuid, deserialized.theGuid);
        }
    }
}
