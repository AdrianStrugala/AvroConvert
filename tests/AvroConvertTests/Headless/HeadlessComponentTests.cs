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
            ExtendedBaseTestClass toSerialize = _fixture.Create<ExtendedBaseTestClass>();
            string schema = AvroConvert.GenerateSchema(typeof(BaseTestClass));

            //Act
            var result = AvroConvert.SerializeHeadless(toSerialize, schema);

            var deserialized = AvroConvert.DeserializeHeadless<BaseTestClass>(result, schema);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.andLongProperty, deserialized.andLongProperty);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
        }

        [Fact]
        public void Component_DeserializeWithMissingFields_NoError()
        {
            //Arrange
            BaseTestClass toSerialize = _fixture.Create<BaseTestClass>();
            string schema = AvroConvert.GenerateSchema(typeof(BaseTestClass));

            //Act
            var result = AvroConvert.SerializeHeadless(toSerialize, schema);

            var deserialized = AvroConvert.DeserializeHeadless<ReducedBaseTestClass>(result, schema);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
        }
    }
}
