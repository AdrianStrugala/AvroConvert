using SolTechnology.Avro;
using AutoFixture;
using Xunit;

namespace AvroConvertTests.Component
{
    public class PrimitiveClassesTests
    {
        private readonly Fixture _fixture;

        public PrimitiveClassesTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Serialize_Object_ResultIsTheSameAsInput()
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
            Assert.Equal(user.favorite_color, deserialized.favorite_color);
            Assert.Equal(user.favorite_number, deserialized.favorite_number);
        }

        [Fact]
        public void Serialize_ObjectContainsGuid_ResultIsTheSameAsInput()
        {
            //Arrange
            ClassWithGuid testClass = _fixture.Create<ClassWithGuid>();

            //Act

            var result = AvroConvert.Serialize(testClass);

            var deserialized = AvroConvert.Deserialize<ClassWithGuid>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testClass.theGuid, deserialized.theGuid);
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
        public void Serialize_Decimal_ResultIsTheSameAsInput()
        {
            //Arrange
            decimal testObject = 21.37m;

            //Act
            var result = AvroConvert.Serialize(testObject);

            var deserialized = AvroConvert.Deserialize<decimal>(result);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(testObject, deserialized);
        }
    }
}