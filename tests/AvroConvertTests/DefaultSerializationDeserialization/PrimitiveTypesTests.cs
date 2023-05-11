using System;
using System.Globalization;
using AutoFixture;
using AutoFixture.Kernel;
using FluentAssertions;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.DefaultSerializationDeserialization
{
    public class PrimitiveTypesTests
    {
        private readonly Fixture _fixture = new();


        [Theory]
        [InlineData(typeof(uint))]
        [InlineData(typeof(ushort))]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(string))]
        [InlineData(typeof(uint?))]
        [InlineData(typeof(ushort?))]
        [InlineData(typeof(ulong?))]
        [InlineData(typeof(ulong))]
        [InlineData(typeof(char))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(sbyte))]
        [InlineData(typeof(bool))]
        [InlineData(typeof(float))]
        [InlineData(typeof(double))]
        [InlineData(typeof(Uri))]
        [InlineData(typeof(byte[]))]
        [InlineData(typeof(decimal))]
        public void Ensure_that_various_primitive_types_are_supported(Type type)
        {
            //Arrange
            var underTest = new SpecimenContext(_fixture).Resolve(type);

            //Act
            var serialized = AvroConvert.Serialize(underTest);
            var deserialized = AvroConvert.Deserialize(serialized, type);

            //Assert
            Assert.NotNull(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal(underTest, deserialized);
        }

        [Fact]
        public void Ensure_that_short_int16_primitive_type_is_supported()
        {
            // Arrange
            var model = new ClassWithShortInt16();
            model.ShortValue = 1;
            model.ShortValueNullable = 1;
            model.UShortValue = 1;
            model.UShortValueNullable = 1;

            // Act
            var serializedModel = AvroConvert.Serialize(model);
            var deserializedModel = AvroConvert.Deserialize<ClassWithShortInt16>(serializedModel);

            // Assert
            Assert.Equivalent(model, deserializedModel);
        }

        public class ClassWithShortInt16
        {
            public short ShortValue { get; set; }
            public short? ShortValueNullable { get; set; }
            public ushort UShortValue { get; set; }
            public ushort? UShortValueNullable { get; set; }
        }

        [Fact]
        public void Component_ObjectWithNulls_ResultIsTheSameAsInput()
        {
            //Arrange
            User user = new User();
            user.name = "Krzys";
            user.favorite_color = null;
            user.favorite_number = null;

            //Act
            var serialized = AvroConvert.Serialize(user);
            var deserialized = AvroConvert.Deserialize<User>(serialized);

            //Assert
            Assert.NotNull(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal(user.name, deserialized.name);
            deserialized.favorite_color.Should().BeNullOrEmpty();
            Assert.Equal(user.favorite_number, deserialized.favorite_number);
        }


        [Theory]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(0)]
        [InlineData(21)]
        [InlineData(-37)]
        public void Component_Int_ResultIsTheSameAsInput(int testObject)
        {
            //Arrange


            //Act
            var result = AvroConvert.Serialize(testObject);
            var deserialized = AvroConvert.Deserialize<int>(result);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(testObject, deserialized);
        }

        [Theory]
        [InlineData(float.NaN)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.MinValue)]
        [InlineData(0.0)]
        [InlineData(float.MaxValue)]
        [InlineData(float.PositiveInfinity)]
        public void Component_Float_ResultIsTheSameAsInput(float testObject)
        {
            //Arrange


            //Act
            var result = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<float>(result);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(testObject, deserialized);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(double.MinValue)]
        [InlineData(0.0)]
        [InlineData(double.MaxValue)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(0)]
        [InlineData(5)]
        public void Component_Double_ResultIsTheSameAsInput(double testObject)
        {
            //Arrange


            //Act
            var result = AvroConvert.Serialize(testObject);
            var deserialized = AvroConvert.Deserialize<double>(result);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(testObject, deserialized);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Component_Bool_ResultIsTheSameAsInput(bool testObject)
        {
            //Arrange


            //Act
            var result = AvroConvert.Serialize(testObject);
            var deserialized = AvroConvert.Deserialize<bool>(result);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(testObject, deserialized);
        }


        [Fact]
        public void Component_ByteArray_ResultIsTheSameAsInput()
        {
            //Arrange
            byte[] testObject = _fixture.Create<byte[]>();

            //Act
            var result = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<byte[]>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testObject, deserialized);
        }

        [Theory]
        [InlineData("")]
        [InlineData("5")]
        [InlineData(" ")]
        [InlineData("π")]
        [InlineData("This is really awesome example of normal string")]
        public void Component_String_ResultIsTheSameAsInput(string testObject)
        {
            //Arrange

            //Act
            var result = AvroConvert.Serialize(testObject);
            var deserialized = AvroConvert.Deserialize<string>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testObject, deserialized);
        }

        [Fact]
        public void Component_Uri_ResultIsTheSameAsInput()
        {
            //Arrange
            var testObject = new Uri("https://dreamtravels.azurewebsites.net");

            //Act
            var result = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<Uri>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testObject, deserialized);
        }

        [Theory]
        [InlineData("21.37")]
        [InlineData("1234.56")]
        [InlineData("-1234.56")]
        [InlineData("-100")]
        [InlineData("100")]
        [InlineData("24.10")]
        [InlineData("27.00")]
        [InlineData("123456789123456789.56")]
        [InlineData("-123456789123456789.56")]
        [InlineData("000000000000000001.01")]
        [InlineData("-000000000000000001.01")]
        [InlineData("-79228162514264337593543950335")]
        [InlineData("79228162514264337593543950335")]
        public void Component_Decimal_ResultIsTheSameAsInput(string test)
        {
            //Arrange
            var testDecimal = decimal.Parse(test, CultureInfo.InvariantCulture);

            //Act
            var result = AvroConvert.Serialize(testDecimal);
            var deserialized = AvroConvert.Deserialize<decimal>(result);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(testDecimal, deserialized);
        }
    }
}