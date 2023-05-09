using System;
using AutoFixture;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class GuidTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Random_Guid(Func<object, Type, dynamic> engine)
        {
            //Arrange
            Guid testClass = _fixture.Create<Guid>();

            //Act
            var deserialized = engine.Invoke(testClass, typeof(Guid));

            //Assert
            Assert.Equal(testClass, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Empty_Guid(Func<object, Type, dynamic> engine)
        {
            //Arrange
            Guid testClass = Guid.Empty;

            //Act
            var deserialized = engine.Invoke(testClass, typeof(Guid));

            //Assert
            Assert.Equal(testClass, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_with_Guid(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithGuid testClass = _fixture.Create<ClassWithGuid>();

            //Act
            var deserialized = engine.Invoke(testClass, typeof(ClassWithGuid));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(testClass.theGuid, deserialized.theGuid);
        }
    }
}
