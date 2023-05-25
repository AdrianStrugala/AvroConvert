using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class EnumTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_with_enum_property(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithEnum toSerialize = _fixture.Create<ClassWithEnum>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(ClassWithEnum));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.EnumProp, deserialized.EnumProp);
            Assert.Equal(toSerialize, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Enum_object(Func<object, Type, dynamic> engine)
        {
            //Arrange
            TestEnum toSerialize = _fixture.Create<TestEnum>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(TestEnum));


            //Assert
            Assert.Equal(toSerialize, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Nullable_enum(Func<object, Type, dynamic> engine)
        {
            //Arrange
            TestEnum? toSerialize = _fixture.Create<TestEnum?>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(TestEnum?));


            //Assert
            Assert.Equal(toSerialize, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void List_of_enums(Func<object, Type, dynamic> engine)
        {
            //Arrange
            List<TestEnum> toSerialize = _fixture.CreateMany<TestEnum>(20).ToList();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(List<TestEnum>));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.Core), MemberType = typeof(TestEngine))]
        public void Dictionary_enum_key_and_value(Func<object, Type, dynamic> engine)
        {
            //Arrange
            Dictionary<TestEnum, TestEnum> toSerialize = _fixture.Create<Dictionary<TestEnum, TestEnum>>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(Dictionary<TestEnum, TestEnum>));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_with_enums_with_default_values(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithEnum toSerialize = _fixture.Create<ClassWithEnum>();
            toSerialize.EnumProp = null;
            toSerialize.SecondEnumProp = null;


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(ClassWithEnum));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(TestEnum.be, deserialized.EnumProp);
            Assert.Equal(TestEnum.ca, deserialized.SecondEnumProp);
        }
    }
}
