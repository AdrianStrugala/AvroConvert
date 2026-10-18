using System;
using System.Collections.Generic;
using AutoFixture;
using AwesomeAssertions;
using NUnit.Framework;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class DictionaryTests
    {
        private readonly Fixture _fixture = new();

        [Test]
        public void AvroMap([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            //Arrange
            Dictionary<string, int> dictionary = _fixture.Create<Dictionary<string, int>>();


            //Act
            var deserialized = RoundTrip.Run<Dictionary<string, int>>(dictionary, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(dictionary);
        }

        [Test]
        public void Dictionary_with_Uri_key_and_object_value([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            //Arrange
            Dictionary<Uri, BaseTestClass> dictionary = _fixture.Create<Dictionary<Uri, BaseTestClass>>();


            //Act
            var deserialized = RoundTrip.Run<Dictionary<Uri, BaseTestClass>>(dictionary, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(dictionary);
        }

        [Test]
        public void Dictionary_Int_Key_Int_Value([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            //Arrange
            Dictionary<int, int> dictionary = _fixture.Create<Dictionary<int, int>>();


            //Act
            var deserialized = RoundTrip.Run<Dictionary<int, int>>(dictionary, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(dictionary);
        }

        [Test]
        public void Class_containing_dictionary_and_map([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            //Arrange
            ExtendedBaseTestClass testClass = _fixture.Create<ExtendedBaseTestClass>();


            //Act
            var deserialized = RoundTrip.Run<ExtendedBaseTestClass>(testClass, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Test]
        [Property("Fix", "https://github.com/AdrianStrugala/AvroConvert/issues/152")]
        public void Class_containing_two_dictionaries([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            //Arrange
            ClassWith2Dictionaries testClass = _fixture.Create<ClassWith2Dictionaries>();


            //Act
            var deserialized = RoundTrip.Run<ClassWith2Dictionaries>(testClass, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Test]
        public void Class_containing_two_maps([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            //Arrange
            ClassWith2Maps testClass = _fixture.Create<ClassWith2Maps>();


            //Act
            var deserialized = RoundTrip.Run<ClassWith2Maps>(testClass, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(testClass);
        }
    }
}
