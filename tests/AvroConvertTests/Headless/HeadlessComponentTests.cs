using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.Headless
{
    public class HeadlessComponentTests
    {
        private readonly Fixture _fixture;

        public HeadlessComponentTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Component_SerializeHeadlessBiggerObjectAndReadSmaller_NoError()
        {
            //Arrange
            VeryComplexClass toSerialize = _fixture.Create<VeryComplexClass>();
            string schema = AvroConvert.GenerateSchema(typeof(VeryComplexClass));

            //Act
            var result = AvroConvert.SerializeHeadless(toSerialize, schema);

            var deserialized = AvroConvert.DeserializeHeadless<VeryComplexClass>(result, schema);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Component_DeserializeWithMissingFields_NoError()
        {
            //Arrange
            SimpleClass toSerialize = _fixture.Create<SimpleClass>();
            string schema = AvroConvert.GenerateSchema(typeof(SimpleClass));

            //Act
            var result = AvroConvert.SerializeHeadless(toSerialize, schema);

            var deserialized = AvroConvert.DeserializeHeadless<SimpleClassWithMissingField>(result, schema);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.StringValue, deserialized.StringValue);
        }
    }
}
