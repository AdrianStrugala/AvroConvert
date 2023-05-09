using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoFixture;
using AutoFixture.Kernel;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class PrimitiveTypesTests
    {
        private readonly Fixture _fixture = new();

        public static IEnumerable<object[]> AllPrimitivesCombinations
        {
            get
            {
                foreach (var c in TestEngine.All().ToList())
                {
                    yield return new[] { typeof(short), c.FirstOrDefault() };
                    yield return new[] { typeof(short?), c.FirstOrDefault() };
                    yield return new[] { typeof(uint), c.FirstOrDefault() };
                    yield return new[] { typeof(ushort), c.FirstOrDefault() };
                    yield return new[] { typeof(int), c.FirstOrDefault() };
                    yield return new[] { typeof(int?), c.FirstOrDefault() };
                    yield return new[] { typeof(long), c.FirstOrDefault() };
                    yield return new[] { typeof(long?), c.FirstOrDefault() };
                    yield return new[] { typeof(Guid), c.FirstOrDefault() };
                    yield return new[] { typeof(Guid?), c.FirstOrDefault() };
                    yield return new[] { typeof(string), c.FirstOrDefault() };
                    yield return new[] { typeof(ulong), c.FirstOrDefault() };
                    yield return new[] { typeof(ulong?), c.FirstOrDefault() };
                    yield return new[] { typeof(char), c.FirstOrDefault() };
                    yield return new[] { typeof(char?), c.FirstOrDefault() };
                    yield return new[] { typeof(byte), c.FirstOrDefault() };
                    yield return new[] { typeof(byte?), c.FirstOrDefault() };
                    yield return new[] { typeof(sbyte), c.FirstOrDefault() };
                    yield return new[] { typeof(sbyte?), c.FirstOrDefault() };
                    yield return new[] { typeof(bool), c.FirstOrDefault() };
                    yield return new[] { typeof(bool?), c.FirstOrDefault() };
                    yield return new[] { typeof(float), c.FirstOrDefault() };
                    yield return new[] { typeof(float?), c.FirstOrDefault() };
                    yield return new[] { typeof(double), c.FirstOrDefault() };
                    yield return new[] { typeof(double?), c.FirstOrDefault() };
                    yield return new[] { typeof(Uri), c.FirstOrDefault() };
                    yield return new[] { typeof(byte[]), c.FirstOrDefault() };
                    yield return new[] { typeof(decimal), c.FirstOrDefault() };
                    yield return new[] { typeof(decimal?), c.FirstOrDefault() };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllPrimitivesCombinations))]
        public void Primitives(Type type, Func<object, Type, dynamic> engine)
        {
            //Arrange
            var underTest = new SpecimenContext(_fixture).Resolve(type);


            //Act
            var deserialized = engine.Invoke(underTest, type);


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(underTest, deserialized);
        }


        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Object_with_null_properties(Func<object, Type, dynamic> engine)
        {
            //Arrange
            User user = new User();
            user.name = "Krzys";
            user.favorite_color = null;
            user.favorite_number = null;

            //Act
            var deserialized = engine.Invoke(user, typeof(User));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(user.name, deserialized.name);
            deserialized.favorite_color.Should().BeNullOrEmpty();
            Assert.Equal(user.favorite_number, deserialized.favorite_number);
        }

        public static IEnumerable<object[]> AllIntCombinations
        {
            get
            {
                foreach (var c in TestEngine.All().ToList())
                {
                    yield return new[] { int.MaxValue, c.FirstOrDefault() };
                    yield return new[] { int.MinValue, c.FirstOrDefault() };
                    yield return new[] { 0, c.FirstOrDefault() };
                    yield return new[] { 21, c.FirstOrDefault() };
                    yield return new[] { -37, c.FirstOrDefault() };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllIntCombinations))]
        public void Ints(int testObject, Func<object, Type, dynamic> engine)
        {
            //Arrange


            //Act
            var deserialized = engine.Invoke(testObject, typeof(int));

            //Assert
            Assert.Equal(testObject, deserialized);
        }

        public static IEnumerable<object[]> AllFloatCombinations
        {
            get
            {
                foreach (var c in TestEngine.All().ToList())
                {
                    yield return new[] { float.NaN, c.FirstOrDefault() };
                    yield return new[] { float.NegativeInfinity, c.FirstOrDefault() };
                    yield return new[] { float.MinValue, c.FirstOrDefault() };
                    yield return new[] { 0.0, c.FirstOrDefault() };
                    yield return new[] { float.MaxValue, c.FirstOrDefault() };
                    yield return new[] { float.PositiveInfinity, c.FirstOrDefault() };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllFloatCombinations))]
        public void Floats(float testObject, Func<object, Type, dynamic> engine)
        {
            //Arrange


            //Act
            var deserialized = engine.Invoke(testObject, typeof(float));

            //Assert
            Assert.Equal(testObject, deserialized);
        }

        public static IEnumerable<object[]> AllDoubleCombinations
        {
            get
            {
                foreach (var c in TestEngine.All().ToList())
                {
                    yield return new[] { double.NaN, c.FirstOrDefault() };
                    yield return new[] { double.NegativeInfinity, c.FirstOrDefault() };
                    yield return new[] { double.MinValue, c.FirstOrDefault() };
                    yield return new[] { 0.0, c.FirstOrDefault() };
                    yield return new[] { double.MaxValue, c.FirstOrDefault() };
                    yield return new[] { double.PositiveInfinity, c.FirstOrDefault() };
                    yield return new[] { 0, c.FirstOrDefault() };
                    yield return new[] { 5, c.FirstOrDefault() };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllDoubleCombinations))]
        public void Doubles(double testObject, Func<object, Type, dynamic> engine)
        {
            //Arrange


            //Act
            var deserialized = engine.Invoke(testObject, typeof(double));

            //Assert
            Assert.Equal(testObject, deserialized);
        }


        public static IEnumerable<object[]> AllBoolCombinations
        {
            get
            {
                foreach (var c in TestEngine.All().ToList())
                {
                    yield return new[] { true, c.FirstOrDefault() };
                    yield return new[] { false, c.FirstOrDefault() };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllBoolCombinations))]
        public void Bools(bool testObject, Func<object, Type, dynamic> engine)
        {
            //Arrange


            //Act
            var deserialized = engine.Invoke(testObject, typeof(bool));

            //Assert
            Assert.Equal(testObject, deserialized);
        }

        public static IEnumerable<object[]> AllStringCombinations
        {
            get
            {
                foreach (var c in TestEngine.All().ToList())
                {
                    yield return new[] { "", c.FirstOrDefault() };
                    yield return new[] { "5", c.FirstOrDefault() };
                    yield return new[] { " ", c.FirstOrDefault() };
                    yield return new[] { "π", c.FirstOrDefault() };
                    yield return new[] { "This is really awesome example of normal string", c.FirstOrDefault() };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllStringCombinations))]
        public void Strings(string testObject, Func<object, Type, dynamic> engine)
        {
            //Arrange

            //Act
            var deserialized = engine.Invoke(testObject, typeof(string));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(testObject, deserialized);
        }

        public static IEnumerable<object[]> AllDecimalCombinations
        {
            get
            {
                foreach (var c in TestEngine.All().ToList())
                {
                    yield return new[] { "21.37", c.FirstOrDefault() };
                    yield return new[] { "1234.56", c.FirstOrDefault() };
                    yield return new[] { "-100", c.FirstOrDefault() };
                    yield return new[] { "100", c.FirstOrDefault() };
                    yield return new[] { "24.10", c.FirstOrDefault() };
                    yield return new[] { "27.00", c.FirstOrDefault() };
                    yield return new[] { "123456789123456789.56", c.FirstOrDefault() };
                    yield return new[] { "-123456789123456789.56", c.FirstOrDefault() };
                    yield return new[] { "000000000000000001.01", c.FirstOrDefault() };
                    yield return new[] { "-000000000000000001.01", c.FirstOrDefault() };
                    yield return new[] { "-79228162514264337593543950335", c.FirstOrDefault() };
                    yield return new[] { "79228162514264337593543950335", c.FirstOrDefault() };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllDecimalCombinations))]
        public void Decimals(string test, Func<object, Type, dynamic> engine)
        {
            //Arrange
            var testDecimal = decimal.Parse(test, CultureInfo.InvariantCulture);

            //Act
            var deserialized = engine.Invoke(testDecimal, typeof(decimal));

            //Assert
            Assert.Equal(testDecimal, deserialized);
        }
    }
}