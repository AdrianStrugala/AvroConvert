using System;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class ClassesWithoutDefaultConstructorTests
    {
        private readonly Fixture _fixture = new();


        [Test]
        public void Class_without_constructor([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithoutDefaultConstructor testClass =
                _fixture.Create<ClassWithoutDefaultConstructor>();


            //Act
            var deserialized = RoundTrip.Run<ClassWithoutDefaultConstructor>(testClass, format);


            //Assert
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Test]
        public void Nested_class_without_constructor([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            VeryComplexClassWithoutDefaultConstructor testClass =
                _fixture.Create<VeryComplexClassWithoutDefaultConstructor>();


            //Act
            var deserialized = RoundTrip.Run<VeryComplexClassWithoutDefaultConstructor>(testClass, format);


            //Assert
            deserialized.Should().BeEquivalentTo(testClass);
        }
    }
}
