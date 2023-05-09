using SolTechnology.Avro;
using System;
using Xunit;

namespace AvroConvertComponentTests.DefaultOnly
{
    public class DifferentReadAndWriteNumberTypesTests
    {
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(short))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        public void Int_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            int value = 1;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(value, deserialized);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(short))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        public void Long_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            long value = 100;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(value, deserialized);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(short))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        public void Short_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            short value = 1;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(value, deserialized);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(short))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        public void Float_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            float value = 1.0f;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            Assert.Equal(value.ToString(), deserialized.ToString());
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(short))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        public void Double_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            double value = 10.00d;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(value.ToString(), deserialized.ToString());
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(long))]
        [InlineData(typeof(short))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        public void Decimal_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            decimal value = 100.000m;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            Assert.NotNull(result);

            Assert.Equal(0, value.CompareTo((decimal)deserialized));
        }
    }
}
