namespace AvroConvertTests.Component
{
    using AutoFixture;
    using AvroConvert;
    using Xunit;

    public class ClassWithFieldsNotPropertiesTests
    {
        private readonly Fixture _fixture;

        public ClassWithFieldsNotPropertiesTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Component_ClassWithoutGetters_ResultIsTheSameAsInput()
        {
            //Arrange
            ClassWithoutGetters toSerialize = _fixture.Create<ClassWithoutGetters>();

            //Act

            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<ClassWithoutGetters>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.Count, deserialized.Count);
            Assert.Equal(toSerialize.SomeString, deserialized.SomeString);
        }

        [Fact]
        public void Component_ComplexClassWithoutGetters_ResultIsTheSameAsInput()
        {
            //Arrange
            ComplexClassWithoutGetters toSerialize = _fixture.Create<ComplexClassWithoutGetters>();

            //Act

            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<ComplexClassWithoutGetters>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Component_ClassWithAttributesAndWithoutGetters_ResultIsTheSameAsInput()
        {
            //Arrange
            AttributeClassWithoutGetters toSerialize = _fixture.Create<AttributeClassWithoutGetters>();

            //Act
            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<AttributeClassWithoutGetters>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }
    }
}
