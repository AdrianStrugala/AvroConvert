using System;
using AutoFixture;
using FluentAssertions;
using Xunit;

namespace AvroConvertComponentTests.SerializationAndDeserialization
{
    public class ClassesWithoutDefaultConstructorTests
    {
        private readonly Fixture _fixture = new();


        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_without_constructor(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithoutDefaultConstructor testClass =
                _fixture.Create<ClassWithoutDefaultConstructor>();


            //Act
            var deserialized = (ClassWithoutDefaultConstructor)engine.Invoke(testClass, typeof(ClassWithoutDefaultConstructor));


            //Assert
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Complex_class_without_constructor(Func<object, Type, dynamic> engine)
        {
            //Arrange
            VeryComplexClassWithoutDefaultConstructor testClass =
                _fixture.Create<VeryComplexClassWithoutDefaultConstructor>();


            //Act
            var deserialized = (VeryComplexClassWithoutDefaultConstructor)engine.Invoke(testClass, typeof(VeryComplexClassWithoutDefaultConstructor));


            //Assert
            deserialized.Should().BeEquivalentTo(testClass);
        }
    }
}
