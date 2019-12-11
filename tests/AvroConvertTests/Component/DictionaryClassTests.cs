using SolTechnology.Avro;

namespace AvroConvertTests.Component
{
    using System;
    using System.Collections.Generic;
    using AutoFixture;
    using Xunit;

    public class DictionaryClassTests
    {
        private readonly Fixture _fixture;

        public DictionaryClassTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Component_ObjectIsAvroMap_ResultIsTheSameAsInput()
        {
            //Arrange
            Dictionary<string, int> dictionary = _fixture.Create<Dictionary<string, int>>();

            //Act

            var result = AvroConvert.Serialize(dictionary);

            var deserialized = AvroConvert.Deserialize<Dictionary<string, int>>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(dictionary, deserialized);
        }

        [Fact]
        public void Component_ObjectIsDictionaryOfComplexTypes_ResultIsTheSameAsInput()
        {
            //Arrange
            Dictionary<Uri, SomeTestClass> dictionary = _fixture.Create<Dictionary<Uri, SomeTestClass>>();

            //Act

            var result = AvroConvert.Serialize(dictionary);

            var deserialized = AvroConvert.Deserialize<Dictionary<Uri, SomeTestClass>>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(dictionary, deserialized);
        }

        [Fact]
        public void Component_ObjectIsDictionary_ResultIsTheSameAsInput()
        {
            //Arrange
            Dictionary<int, int> dictionary = _fixture.Create<Dictionary<int, int>>();

            //Act

            var result = AvroConvert.Serialize(dictionary);

            var deserialized = AvroConvert.Deserialize<Dictionary<int, int>>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(dictionary, deserialized);
        }

        [Fact]
        public void Component_ObjectContainsDictionaryAndMap_ResultIsTheSameAsInput()
        {
            //Arrange
            BiggerNestedTestClass testClass = _fixture.Create<BiggerNestedTestClass>();


            //Act
            var result = AvroConvert.Serialize(testClass);

            var deserialized = AvroConvert.Deserialize<BiggerNestedTestClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testClass, deserialized);
        }
    }
}
