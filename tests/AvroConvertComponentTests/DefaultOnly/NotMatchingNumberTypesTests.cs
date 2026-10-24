using AwesomeAssertions;
using SolTechnology.Avro;
using System;
using NUnit.Framework;

namespace AvroConvertComponentTests.DefaultOnly
{
    public class DifferentReadAndWriteNumberTypesTests
    {
        [TestCase(typeof(int))]
        [TestCase(typeof(long))]
        [TestCase(typeof(short))]
        [TestCase(typeof(decimal))]
        [TestCase(typeof(double))]
        [TestCase(typeof(float))]
        public void Int_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            int value = 1;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            result.Should().NotBeNull();
            ((object)deserialized).Should().Be(value);
        }

        [TestCase(typeof(int))]
        [TestCase(typeof(long))]
        [TestCase(typeof(short))]
        [TestCase(typeof(decimal))]
        [TestCase(typeof(double))]
        [TestCase(typeof(float))]
        public void Long_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            long value = 100;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            result.Should().NotBeNull();
            ((object)deserialized).Should().Be(value);
        }

        [TestCase(typeof(int))]
        [TestCase(typeof(long))]
        [TestCase(typeof(short))]
        [TestCase(typeof(decimal))]
        [TestCase(typeof(double))]
        [TestCase(typeof(float))]
        public void Short_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            short value = 1;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            result.Should().NotBeNull();
            ((object)deserialized).Should().Be(value);
        }

        [TestCase(typeof(int))]
        [TestCase(typeof(long))]
        [TestCase(typeof(short))]
        [TestCase(typeof(decimal))]
        [TestCase(typeof(double))]
        [TestCase(typeof(float))]
        public void Float_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            float value = 1.0f;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            ((object)deserialized.ToString()).Should().Be(value.ToString());
        }

        [TestCase(typeof(int))]
        [TestCase(typeof(long))]
        [TestCase(typeof(short))]
        [TestCase(typeof(decimal))]
        [TestCase(typeof(double))]
        [TestCase(typeof(float))]
        public void Double_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            double value = 10.00d;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            result.Should().NotBeNull();
            ((object)deserialized.ToString()).Should().Be(value.ToString());
        }

        [TestCase(typeof(int))]
        [TestCase(typeof(long))]
        [TestCase(typeof(short))]
        [TestCase(typeof(decimal))]
        [TestCase(typeof(double))]
        [TestCase(typeof(float))]
        public void Decimal_Can_Be_Deserialized_To_Different_Number_Types(Type readType)
        {
            //Arrange
            decimal value = 100.000m;

            //Act
            var result = AvroConvert.Serialize(value);
            var deserialized = AvroConvert.Deserialize(result, readType);

            //Assert
            result.Should().NotBeNull();

            value.CompareTo((decimal)deserialized).Should().Be(0);
        }
    }
}
