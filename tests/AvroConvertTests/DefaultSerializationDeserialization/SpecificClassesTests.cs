using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.DefaultSerializationDeserialization
{
    public class SpecificClassesTests
    {
        private readonly Fixture _fixture;

        public SpecificClassesTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Component_NetRecord_ResultIsTheSameAsInput()
        {
            //Arrange
            TestRecord testRecord = new TestRecord();
            testRecord.Name = _fixture.Create<string>();

            //Act
            var serialized = AvroConvert.Serialize(testRecord);

            var deserialized = AvroConvert.Deserialize<TestRecord>(serialized);

            //Assert
            Assert.NotNull(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal(testRecord.Name, deserialized.Name);
        }

        [Fact]
        public void Component_AvroLogicalType_ResultIsTheSameAsInput()
        {
            //Arrange
            var testObject = _fixture.Create<LogicalTypesClass>();

            //Act
            var serialized = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<LogicalTypesClass>(serialized);

            //Assert
            Assert.NotNull(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal(testObject.One, deserialized.One);
            Assert.Equal(testObject.Two, deserialized.Two);
            Assert.Equal(testObject.Three.Milliseconds, deserialized.Three.Milliseconds);
            Assert.Equal(testObject.Three.Seconds, deserialized.Three.Seconds);
            Assert.Equal(testObject.Three.Minutes, deserialized.Three.Minutes);
            Assert.Equal(testObject.Three.Hours, deserialized.Three.Hours);
            Assert.Equal(testObject.Three.Days, deserialized.Three.Days);
            Assert.Equal(testObject.Four, deserialized.Four);
            Assert.Equal(testObject.Five, deserialized.Five);
        }
    }
}
