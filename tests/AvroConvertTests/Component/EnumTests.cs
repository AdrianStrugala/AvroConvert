using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertTests.Component
{
    public class EnumTests
    {
        private readonly Fixture _fixture;

        public EnumTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Component_SerializeEnumClass_ResultIsTheSameAsInput()
        {
            //Arrange
            ClassWithEnum toSerialize = _fixture.Create<ClassWithEnum>();


            //Act
            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<ClassWithEnum>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.enumVariable, deserialized.enumVariable);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Component_SerializeEnum_ResultIsTheSameAsInput()
        {
            //Arrange
            TestEnum toSerialize = _fixture.Create<TestEnum>();


            //Act
            var result = AvroConvert.Serialize(toSerialize);

            TestEnum deserialized = AvroConvert.Deserialize<TestEnum>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Component_SerializeListOfEnums_ResultIsTheSameAsInput()
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
    }
}
