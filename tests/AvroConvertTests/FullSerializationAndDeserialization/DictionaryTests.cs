using System;
using System.Collections.Generic;
using AutoFixture;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class DictionaryTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.Core), MemberType = typeof(TestEngine))]
        public void AvroMap(Func<object, Type, dynamic> engine)
        {
            //Arrange
            Dictionary<string, int> dictionary = _fixture.Create<Dictionary<string, int>>();


            //Act
            var deserialized = engine.Invoke(dictionary, typeof(Dictionary<string, int>));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(dictionary, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.Core), MemberType = typeof(TestEngine))]
        public void Dictionary_with_Uri_key_and_object_value(Func<object, Type, dynamic> engine)
        {
            //Arrange
            Dictionary<Uri, BaseTestClass> dictionary = _fixture.Create<Dictionary<Uri, BaseTestClass>>();


            //Act
            var deserialized = engine.Invoke(dictionary, typeof(Dictionary<Uri, BaseTestClass>));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(dictionary, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.Core), MemberType = typeof(TestEngine))]
        public void Dictionary_Int_Key_Int_Value(Func<object, Type, dynamic> engine)
        {
            //Arrange
            Dictionary<int, int> dictionary = _fixture.Create<Dictionary<int, int>>();


            //Act
            var deserialized = engine.Invoke(dictionary, typeof(Dictionary<int, int>));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(dictionary, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.Core), MemberType = typeof(TestEngine))]
        public void Class_containing_dictionary_and_map(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ExtendedBaseTestClass testClass = _fixture.Create<ExtendedBaseTestClass>();


            //Act
            var deserialized = engine.Invoke(testClass, typeof(ExtendedBaseTestClass));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(testClass, deserialized);
        }
    }
}
