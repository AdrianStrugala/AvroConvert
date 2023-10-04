using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class ContainerTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.ContainerOnly), MemberType = typeof(TestEngine))]
        public void AvroMap(Func<object, Type, dynamic> engine)
        {
            //Arrange
            List<int> expected = new List<int>() { 1, 2, 3, 5, 8, 13 };


            //Act
            var actual = engine.Invoke(expected, typeof(List<int>));


            //Assert
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(TestEngine.ContainerOnly), MemberType = typeof(TestEngine))]
        public void List_of_class_map(Func<object, Type, dynamic> engine)
        {
            //Arrange
            List<ExtendedBaseTestClass> expected = _fixture.CreateMany<ExtendedBaseTestClass>(5).ToList();


            //Act
            var actual = engine.Invoke(expected, typeof(List<ExtendedBaseTestClass>));


            //Assert
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(TestEngine.ContainerOnly), MemberType = typeof(TestEngine))]
        public void Array_of_class_map(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ExtendedBaseTestClass[] expected = _fixture.CreateMany<ExtendedBaseTestClass>(5).ToArray();


            //Act
            var actual = engine.Invoke(expected, typeof(ExtendedBaseTestClass[]));


            //Assert
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(TestEngine.ContainerOnly), MemberType = typeof(TestEngine))]
        public void Enumerable_of_class_map_to_list(Func<object, Type, dynamic> engine)
        {
            //Arrange
            IEnumerable<ExtendedBaseTestClass> expected = _fixture.CreateMany<ExtendedBaseTestClass>(5);


            //Act
            var actual = engine.Invoke(expected, typeof(List<ExtendedBaseTestClass>));


            //Assert
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(TestEngine.ContainerOnly), MemberType = typeof(TestEngine))]
        public void Enumerable_of_class_map_to_array(Func<object, Type, dynamic> engine)
        {
            //Arrange
            IEnumerable<ExtendedBaseTestClass> expected = _fixture.CreateMany<ExtendedBaseTestClass>(5);


            //Act
            var actual = engine.Invoke(expected, typeof(ExtendedBaseTestClass[]));


            //Assert
            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }
    }
}