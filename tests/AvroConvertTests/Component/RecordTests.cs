using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertTests.Component
{
    public class RecordTests
    {
        private readonly Fixture _fixture;

        public RecordTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Serialize_ObjectWithNulls_ResultIsTheSameAsInput()
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
    }
}
