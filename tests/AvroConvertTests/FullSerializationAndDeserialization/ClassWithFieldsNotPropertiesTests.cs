using System;
using AutoFixture;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class ClassWithFieldsNotPropertiesTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Simple_class_with_fields(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithoutGetters toSerialize = _fixture.Create<ClassWithoutGetters>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(ClassWithoutGetters));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.Count, deserialized.Count);
            Assert.Equal(toSerialize.SomeString, deserialized.SomeString);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Nested_class_with_fields(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ComplexClassWithoutGetters toSerialize = _fixture.Create<ComplexClassWithoutGetters>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(ComplexClassWithoutGetters));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Nested_class_with_fields_and_attributes(Func<object, Type, dynamic> engine)
        {
            //Arrange
            AttributeClassWithoutGetters toSerialize = _fixture.Create<AttributeClassWithoutGetters>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(AttributeClassWithoutGetters));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }
    }
}
