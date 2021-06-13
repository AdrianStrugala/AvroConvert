using System;
using System.Collections.Generic;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.DefaultSerializationDeserialization
{
    public class DictionaryTests
    {
        private readonly Fixture _fixture;

        public DictionaryTests()
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
            Dictionary<Uri, BaseTestClass> dictionary = _fixture.Create<Dictionary<Uri, BaseTestClass>>();

            //Act

            var result = AvroConvert.Serialize(dictionary);

            var deserialized = AvroConvert.Deserialize<Dictionary<Uri, BaseTestClass>>(result);

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
            ExtendedBaseTestClass testClass = _fixture.Create<ExtendedBaseTestClass>();


            //Act
            var result = AvroConvert.Serialize(testClass);

            var deserialized = AvroConvert.Deserialize<ExtendedBaseTestClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testClass, deserialized);
        }
    }
}
