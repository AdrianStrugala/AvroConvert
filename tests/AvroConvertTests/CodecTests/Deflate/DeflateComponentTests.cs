using AutoFixture;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using Xunit;

namespace AvroConvertComponentTests.CodecTests.Deflate
{
    public class DeflateComponentTests
    {
        private readonly Fixture _fixture;

        public DeflateComponentTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Deflate_SerializeAndDeserializeComplexClass_NoError()
        {
            //Arrange
            ExtendedBaseTestClass toSerialize = _fixture.Create<ExtendedBaseTestClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize, CodecType.Deflate);

            var deserialized = AvroConvert.Deserialize<ExtendedBaseTestClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Deflate_SerializeBiggerObjectAndReadSmaller_NoError()
        {
            //Arrange
            ExtendedBaseTestClass toSerialize = _fixture.Create<ExtendedBaseTestClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize, CodecType.Deflate);

            var deserialized = AvroConvert.Deserialize<BaseTestClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
            Assert.Equal(toSerialize.andLongProperty, deserialized.andLongProperty);
        }


        [Fact]
        public void Deflate_SerializeSmallerClassAndReadBigger_NoError()
        {
            //Arrange
            ReducedBaseTestClass toSerialize = _fixture.Create<ReducedBaseTestClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize, CodecType.Deflate);

            var deserialized = AvroConvert.Deserialize<BaseTestClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
        }

        [Fact]
        public void Deflate_SerializeBiggerAvroObjectAndReadSmaller_NoError()
        {
            //Arrange
            AttributeClass toSerialize = _fixture.Create<AttributeClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize, CodecType.Deflate);

            var deserialized = AvroConvert.Deserialize<SmallerAttributeClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.StringProperty, deserialized.StringProperty);
            Assert.Equal(toSerialize.NullableIntProperty, deserialized.NullableIntProperty);
        }
    }
}
