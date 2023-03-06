using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.DefaultSerializationDeserialization
{
    public class EnumTests
    {
        private readonly Fixture _fixture;

        public EnumTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Component_EnumProperty_ResultIsTheSameAsInput()
        {
            //Arrange
            ClassWithEnum toSerialize = _fixture.Create<ClassWithEnum>();


            //Act
            var result = AvroConvert.Serialize(toSerialize);
            var deserialized = AvroConvert.Deserialize<ClassWithEnum>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.EnumProp, deserialized.EnumProp);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Component_Enum_ResultIsTheSameAsInput()
        {
            //Arrange
            TestEnum toSerialize = _fixture.Create<TestEnum>();


            //Act
            var result = AvroConvert.Serialize(toSerialize);
            TestEnum deserialized = AvroConvert.Deserialize<TestEnum>(result);


            //Assert
            Assert.NotNull(result);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Component_NullableEnum_ResultIsTheSameAsInput()
        {
            //Arrange
            TestEnum? toSerialize = _fixture.Create<TestEnum?>();


            //Act
            var result = AvroConvert.Serialize(toSerialize);
            TestEnum? deserialized = AvroConvert.Deserialize<TestEnum?>(result);


            //Assert
            Assert.NotNull(result);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Component_ListOfEnums_ResultIsTheSameAsInput()
        {
            //Arrange
            List<TestEnum> toSerialize = _fixture.CreateMany<TestEnum>(20).ToList();


            //Act
            var result = AvroConvert.Serialize(toSerialize);
            var deserialized = AvroConvert.Deserialize<List<TestEnum>>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Component_DictionaryOfEnums_ResultIsTheSameAsInput()
        {
            //Arrange
            Dictionary<TestEnum, TestEnum> toSerialize = _fixture.Create<Dictionary<TestEnum, TestEnum>>();


            //Act
            var result = AvroConvert.Serialize(toSerialize);
            var deserialized = AvroConvert.Deserialize<Dictionary<TestEnum, TestEnum>>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Component_EnumPropertyWithDefaultValue_ResultIsDefaultValue()
        {
            //Arrange
            ClassWithEnum toSerialize = _fixture.Create<ClassWithEnum>();
            toSerialize.EnumProp = null;
            toSerialize.SecondEnumProp = null;


            //Act
            var result = AvroConvert.Serialize(toSerialize);
            var deserialized = AvroConvert.Deserialize<ClassWithEnum>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(TestEnum.be, deserialized.EnumProp);
            Assert.Equal(TestEnum.ca, deserialized.SecondEnumProp);
        }
    }
}
