using System;
using AutoFixture;
using Xunit;

namespace AvroConvertComponentTests.DefaultOnly
{
    public class AvroAttributeClassTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.DefaultOnly), MemberType = typeof(TestEngine))]
        public void Types_mismatches_are_handled(Func<object, Type, dynamic> engine)
        {
            //Arrange
            UserWithNonNullableProperties toSerialize = _fixture.Create<UserWithNonNullableProperties>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(User));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.favorite_number, deserialized.favorite_number);
            Assert.Equal(toSerialize.name, deserialized.name);
            Assert.Equal(toSerialize.favorite_color, deserialized.favorite_color);
        }
    }
}
