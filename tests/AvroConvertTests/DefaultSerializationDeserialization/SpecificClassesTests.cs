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

            Assert.NotNull(testObject.Four);
            Assert.NotNull(deserialized.Four);
            Assert.Equal(testObject.Four.Value.Millisecond, deserialized.Four.Value.Millisecond);
            Assert.Equal(testObject.Four.Value.Second, deserialized.Four.Value.Second);
            Assert.Equal(testObject.Four.Value.Minute, deserialized.Four.Value.Minute);
            Assert.Equal(testObject.Four.Value.Hour, deserialized.Four.Value.Hour);
            Assert.Equal(testObject.Four.Value.Day, deserialized.Four.Value.Day);
            Assert.Equal(testObject.Four.Value.Month, deserialized.Four.Value.Month);
            Assert.Equal(testObject.Four.Value.Year, deserialized.Four.Value.Year);

            Assert.Equal(testObject.Five.Millisecond, deserialized.Five.Millisecond);
            Assert.Equal(testObject.Five.Second, deserialized.Five.Second);
            Assert.Equal(testObject.Five.Minute, deserialized.Five.Minute);
            Assert.Equal(testObject.Five.Hour, deserialized.Five.Hour);
            Assert.Equal(testObject.Five.Day, deserialized.Five.Day);
            Assert.Equal(testObject.Five.Month, deserialized.Five.Month);
            Assert.Equal(testObject.Five.Year, deserialized.Five.Year);
        }
    }
}
