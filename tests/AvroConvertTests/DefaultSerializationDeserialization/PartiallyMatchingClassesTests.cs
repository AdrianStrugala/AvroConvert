using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.DefaultSerializationDeserialization
{
    public class PartiallyMatchingClassesTests
    {
        private readonly Fixture _fixture;

        public PartiallyMatchingClassesTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Component_SerializeBiggerObjectAndReadSmaller_NoError()
        {
            //Arrange
            ExtendedBaseTestClass toSerialize = _fixture.Create<ExtendedBaseTestClass>();

            //Act

            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<BaseTestClass>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
            Assert.Equal(toSerialize.andLongProperty, deserialized.andLongProperty);
        }


        [Fact]
        public void Component_SerializeSmallerClassAndReadBigger_NoError()
        {
            //Arrange
            ReducedBaseTestClass toSerialize = _fixture.Create<ReducedBaseTestClass>();

            //Act

            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<BaseTestClass>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
        }

        [Fact]
        public void Component_SerializeBiggerAvroObjectAndReadSmaller_NoError()
        {
            //Arrange
            AttributeClass toSerialize = _fixture.Create<AttributeClass>();

            //Act

            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<SmallerAttributeClass>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.StringProperty, deserialized.StringProperty);
            Assert.Equal(toSerialize.NullableIntProperty, deserialized.NullableIntProperty);
        }

        [Fact]
        public void Component_SerializeAndDeserializeClassesWithDifferentPropertyCases_NoError()
        {
            //Arrange
            BaseTestClass toSerialize = _fixture.Create<BaseTestClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<DifferentCaseBaseTestClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.JustSomeProperty);
            Assert.Equal(toSerialize.andLongProperty, deserialized.AndLongProperty);
        }
    }
}
