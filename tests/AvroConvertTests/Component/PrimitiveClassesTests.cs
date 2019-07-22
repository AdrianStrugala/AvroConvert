namespace AvroConvertTests.Component
{
    using AutoFixture;
    using AvroConvert;
    using Xunit;

    public class PrimitiveClassesTests
    {
        private readonly Fixture _fixture;

        public PrimitiveClassesTests()
        {
            _fixture = new Fixture();
        }
        [Fact]
        public void Serialize_ThenDeserialize_ObjectsAreEqual()
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
            ClassWithGuid
                testClass = _fixture.Create<ClassWithGuid>();

            //Act

            var result = AvroConvert.Serialize(testClass);

            var deserialized = AvroConvert.Deserialize<ClassWithGuid>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testClass.theGuid, deserialized.theGuid);
        }
    }
}
