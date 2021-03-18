using System;
using System.Globalization;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.DefaultSerializationDeserialization
{
    public class PrimitiveClassesTests
    {
        private readonly Fixture _fixture;

        public PrimitiveClassesTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Serialize_ObjectWithNulls_ResultIsTheSameAsInput()
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
            Assert.True(Comparison.AreEqual(user.favorite_color, deserialized.favorite_color));
            Assert.Equal(user.favorite_number, deserialized.favorite_number);
        }

        [Fact]
        public void Serialize_ZeroInt_ResultIsTheSameAsInput()
        {
            //Arrange
            int testObject = 0;

            //Act
            var result = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<int>(result);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(testObject, deserialized);
        }

        [Fact]
        public void Serialize_MaxInt_ResultIsTheSameAsInput()
        {
            //Arrange
            int testObject = int.MaxValue;

            //Act
            var result = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<int>(result);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(testObject, deserialized);
        }

        [Fact]
        public void Serialize_MinInt_ResultIsTheSameAsInput()
        {
            //Arrange
            int testObject = int.MinValue;

            //Act
            var result = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<int>(result);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(testObject, deserialized);
        }

        [Fact]
        public void Serialize_BoolTrue_ResultIsTheSameAsInput()
        {
            //Arrange
            bool testObject = true;

            //Act
            var result = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<bool>(result);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(testObject, deserialized);
        }

        [Fact]
        public void Serialize_BoolFalse_ResultIsTheSameAsInput()
        {
            //Arrange
            bool testObject = false;

            //Act
            var result = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<bool>(result);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(testObject, deserialized);
        }

        [Fact]
        public void Serialize_ByteArray_ResultIsTheSameAsInput()
        {
            //Arrange
            var testObject = new byte[]
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
                7,
                8,
                9
            };

            //Act
            var result = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<byte[]>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testObject, deserialized);
        }

        [Fact]
        public void Serialize_EmptyString_ResultIsTheSameAsInput()
        {
            //Arrange
            var testObject = "";

            //Act
            var result = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<string>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testObject, deserialized);
        }

        [Fact]
        public void Serialize_Uri_ResultIsTheSameAsInput()
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
        public void Serialize_Decimal_ResultIsTheSameAsInput(string test)
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