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
            BiggerNestedTestClass toSerialize = _fixture.Create<BiggerNestedTestClass>();

            //Act

            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<NestedTestClass>(result);

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
            SmallerNestedTestClass toSerialize = _fixture.Create<SmallerNestedTestClass>();

            //Act

            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<NestedTestClass>(result);

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
            NestedTestClass toSerialize = _fixture.Create<NestedTestClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<DifferentCaseNestedTestClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.JustSomeProperty);
            Assert.Equal(toSerialize.andLongProperty, deserialized.AndLongProperty);
        }
    }
}
