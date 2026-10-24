using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoFixture;
using AutoFixture.Kernel;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class PrimitiveTypesTests
    {
        private readonly Fixture _fixture = new();

        public static IEnumerable<Type> PrimitiveTypes => new[]
        {
            typeof(short),
            typeof(short?),
            typeof(uint),
            typeof(ushort),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?),
            typeof(Guid),
            typeof(Guid?),
            typeof(string),
            typeof(ulong),
            typeof(ulong?),
            typeof(char),
            typeof(char?),
            typeof(byte),
            typeof(byte?),
            typeof(sbyte),
            typeof(sbyte?),
            typeof(bool),
            typeof(bool?),
            typeof(float),
            typeof(float?),
            typeof(double),
            typeof(double?),
            typeof(Uri),
            typeof(byte[]),
            typeof(decimal),
            typeof(decimal?)
        };

        [Test]
        public void Primitives([ValueSource(nameof(PrimitiveTypes))] Type type, [ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            var underTest = new SpecimenContext(_fixture).Resolve(type);


            //Act
            var deserialized = RoundTrip.Run(underTest, type, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(underTest);
        }


        [Test]
        public void Object_with_null_properties([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            User user = new User();
            user.name = null;
            user.favorite_color = "green";
            user.favorite_number = null;

            //Act
            var deserialized = RoundTrip.Run<User>(user, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.name.Should().BeNull();
            deserialized.favorite_color.Should().Be(user.favorite_color);
            deserialized.favorite_number.Should().BeNull();
        }

        [Test]
        public void Null_in_member_that_does_not_allow_null_throws([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            User user = new User { name = "Krzys", favorite_number = 7, favorite_color = null };

            //Act
            var exception = Recorded.Exception(() => RoundTrip.Run<User>(user, format));

            //Assert
            exception.Should().NotBeNull();
            exception.Message.Should().Contain("Field 'User.favorite_color' is null")
                .And.Contain("\"string\"")
                .And.Contain(nameof(AvroNullValueHandling.WriteEmpty));
        }

        [Test]
        public void Null_in_member_that_does_not_allow_null_with_WriteEmpty_is_written_as_empty_value([ValueSource(typeof(Formats), nameof(Formats.Evolution))] Format format)
        {
            //Arrange
            User user = new User { name = "Krzys", favorite_number = null, favorite_color = null };
            var options = new AvroConvertOptions { NullValueHandling = AvroNullValueHandling.WriteEmpty };
            if (format == Format.Brotli) options.Codec = CodecType.Brotli;

            //Act
            var deserialized = RoundTrip.Run<User>(user, format == Format.Brotli ? Format.Container : format, options);

            //Assert
            deserialized.name.Should().Be(user.name);
            deserialized.favorite_number.Should().BeNull();
            deserialized.favorite_color.Should().BeEmpty();
        }

        [Test]
        public void Ints([Values(int.MaxValue, int.MinValue, 0, 21, -37)] int testObject, [ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            RoundTrip.Run<int>(testObject, format).Should().Be(testObject);
        }

        [Test]
        public void Floats([Values(float.NaN, float.NegativeInfinity, float.MinValue, 0f, float.MaxValue, float.PositiveInfinity)] float testObject,
            [ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            RoundTrip.Run<float>(testObject, format).Should().Be(testObject);
        }

        [Test]
        public void Doubles([Values(double.NaN, double.NegativeInfinity, double.MinValue, 0d, double.MaxValue, double.PositiveInfinity, 5d)] double testObject,
            [ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            RoundTrip.Run<double>(testObject, format).Should().Be(testObject);
        }

        [Test]
        public void Bools([Values] bool testObject, [ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            RoundTrip.Run<bool>(testObject, format).Should().Be(testObject);
        }

        [Test]
        public void Strings([Values("", "5", " ", "π", "This is really awesome example of normal string")] string testObject,
            [ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            var deserialized = RoundTrip.Run<string>(testObject, format);

            deserialized.Should().NotBeNull();
            deserialized.Should().Be(testObject);
        }

        [Test]
        public void Decimals(
            [Values("21.37", "1234.56", "-100", "100", "24.10", "27.00", "123456789123456789.56", "-123456789123456789.56",
                "000000000000000001.01", "-000000000000000001.01", "-79228162514264337593543950335", "79228162514264337593543950335")] string test,
            [ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            var testDecimal = decimal.Parse(test, CultureInfo.InvariantCulture);

            RoundTrip.Run<decimal>(testDecimal, format).Should().Be(testDecimal);
        }
    }
}