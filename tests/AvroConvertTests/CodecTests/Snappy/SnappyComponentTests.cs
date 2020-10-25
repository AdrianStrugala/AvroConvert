using AutoFixture;
using SolTechnology.Avro;
using SolTechnology.Avro.FileHeader.Codec;
using Xunit;

namespace AvroConvertTests.CodecTests.Snappy
{
    public class SnappyComponentTests
    {
        private readonly Fixture _fixture;

        public SnappyComponentTests()
        {
            _fixture = new Fixture();
        }


        [Fact]
        public void Snappy_SerializeAndDeserializeComplexClass_NoError()
        {
            //Arrange
            BiggerNestedTestClass toSerialize = _fixture.Create<BiggerNestedTestClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize, CodecType.Snappy);

            var deserialized = AvroConvert.Deserialize<BiggerNestedTestClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Snappy_SerializeBiggerObjectAndReadSmaller_NoError()
        {
            //Arrange
            BiggerNestedTestClass toSerialize = _fixture.Create<BiggerNestedTestClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize, CodecType.Snappy);

            var deserialized = AvroConvert.Deserialize<NestedTestClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
            Assert.Equal(toSerialize.andLongProperty, deserialized.andLongProperty);
        }


        [Fact]
        public void Snappy_SerializeSmallerClassAndReadBigger_NoError()
        {
            //Arrange
            SmallerNestedTestClass toSerialize = _fixture.Create<SmallerNestedTestClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize, CodecType.Snappy);

            var deserialized = AvroConvert.Deserialize<NestedTestClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
        }

        [Fact]
        public void Snappy_SerializeBiggerAvroObjectAndReadSmaller_NoError()
        {
            //Arrange
            AttributeClass toSerialize = _fixture.Create<AttributeClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize, CodecType.Snappy);

            var deserialized = AvroConvert.Deserialize<SmallerAttributeClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.StringProperty, deserialized.StringProperty);
            Assert.Equal(toSerialize.NullableIntProperty, deserialized.NullableIntProperty);
        }
    }
}
