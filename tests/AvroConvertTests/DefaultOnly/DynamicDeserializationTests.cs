using SolTechnology.Avro;
using System;
using System.Linq;
using AutoFixture;
using AutoFixture.Kernel;
using Xunit;

namespace AvroConvertComponentTests.DefaultOnly
{
    public class DynamicDeserializationTests
    {
        readonly Fixture _fixture = new();

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(string))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        [InlineData(typeof(Uri))]
        [InlineData(typeof(Guid))]
        public void Dynamic_deserialization_works_for_primitives(Type type)
        {
            //Arrange
            var input = new SpecimenContext(_fixture).Resolve(type);

            //Act
            var result = AvroConvert.Serialize(input);

            var deserialized = AvroConvert.Deserialize<dynamic>(result);

            //Assert
            Assert.NotNull(result);

            Assert.Equal(input, deserialized);
        }

        [Fact]
        public void Dynamic_deserialization_works_for_complex_types()
        {
            //Arrange
            var input = _fixture.Create<User>();

            //Act
            var result = AvroConvert.Serialize(input);

            var deserialized = AvroConvert.Deserialize<dynamic>(result);

            //Assert
            Assert.NotNull(result);

            Assert.Equal(input.favorite_color, deserialized.favorite_color);
            Assert.Equal(input.favorite_number, deserialized.favorite_number);
            Assert.Equal(input.name, deserialized.name);
        }

        [Fact]
        public void Dynamic_deserialization_works_for_very_complex_types()
        {
            //Arrange
            var input = _fixture.Create<VeryComplexClass>();

            //Act
            var result = AvroConvert.Serialize(input);

            var deserialized = AvroConvert.Deserialize<dynamic>(result);

            //Assert
            Assert.NotNull(result);

            Assert.Equal(input.ClassesWithArray[0].theArray[0], deserialized.ClassesWithArray[0].theArray[0]);
            Assert.Equal(input.ClassesWithGuid[0].theGuid, deserialized.ClassesWithGuid[0].theGuid);
            Assert.Equal(input.floatProperty, deserialized.floatProperty);
            Assert.Equal(input.Size.Value, deserialized.Size);
        }
    }
}
