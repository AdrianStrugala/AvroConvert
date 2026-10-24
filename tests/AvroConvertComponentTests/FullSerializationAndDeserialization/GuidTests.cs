using AwesomeAssertions;
using System;
using AutoFixture;
using NUnit.Framework;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class GuidTests
    {
        private readonly Fixture _fixture = new();

        [Test]
        public void Random_Guid([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            Guid testClass = _fixture.Create<Guid>();

            //Act
            var deserialized = RoundTrip.Run<Guid>(testClass, format);

            //Assert
            deserialized.Should().Be(testClass);
        }

        [Test]
        public void Empty_Guid([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            Guid testClass = Guid.Empty;

            //Act
            var deserialized = RoundTrip.Run<Guid>(testClass, format);

            //Assert
            deserialized.Should().Be(testClass);
        }

        [Test]
        public void Class_with_Guid([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithGuid testClass = _fixture.Create<ClassWithGuid>();

            //Act
            var deserialized = RoundTrip.Run<ClassWithGuid>(testClass, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.theGuid.Should().Be(testClass.theGuid);
        }
    }
}
