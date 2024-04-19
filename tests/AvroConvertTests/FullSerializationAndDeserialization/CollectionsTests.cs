using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class CollectionsTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void List_of_class(Func<object, Type, dynamic> engine)
        {
            //Arrange
            var someTestClasses = _fixture.CreateMany<BaseTestClass>().ToList();

            //Act
            var deserialized = engine.Invoke(someTestClasses, typeof(List<BaseTestClass>));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(someTestClasses, deserialized);
        }

        [Theory]
        [Trait("Fix", "https://github.com/AdrianStrugala/AvroConvert/issues/146")]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void IEnumerable_of_class(Func<object, Type, dynamic> engine)
        {
            //Arrange
            var someTestClasses = _fixture.CreateMany<BaseTestClass>();

            //Act
            var deserialized = engine.Invoke(someTestClasses, typeof(IEnumerable<BaseTestClass>));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(someTestClasses, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_with_list(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithSimpleList testClass = _fixture.Create<ClassWithSimpleList>();

            //Act
            var deserialized = (ClassWithSimpleList)engine.Invoke(testClass, typeof(ClassWithSimpleList));

            //Assert
            Assert.NotNull(deserialized);
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_with_list_of_classes(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithConstructorPopulatingProperty
                testClass = _fixture.Create<ClassWithConstructorPopulatingProperty>();

            //Act
            var deserialized = (ClassWithConstructorPopulatingProperty)engine.Invoke(testClass, typeof(ClassWithConstructorPopulatingProperty));

            //Assert
            Assert.NotNull(deserialized);
            deserialized.Should().BeEquivalentTo(testClass);

        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_with_array(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithArray
                testClass = _fixture.Create<ClassWithArray>();

            //Act
            var deserialized = (ClassWithArray)engine.Invoke(testClass, typeof(ClassWithArray));

            //Assert
            Assert.NotNull(deserialized);
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void List_of_int(Func<object, Type, dynamic> engine)
        {
            //Arrange
            List<int> toSerialize = _fixture.Create<List<int>>();

            //Act
            var deserialized = (List<int>)engine.Invoke(toSerialize, typeof(List<int>));

            //Assert
            Assert.NotNull(deserialized);
            deserialized.Should().BeEquivalentTo(deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Array_of_int(Func<object, Type, dynamic> engine)
        {
            //Arrange
            int[] array = _fixture.Create<int[]>();

            //Act
            var deserialized = (List<int>)engine.Invoke(array, typeof(List<int>));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(array, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void HashSet_of_int(Func<object, Type, dynamic> engine)
        {
            //Arrange
            HashSet<int> hashset = _fixture.Create<HashSet<int>>();

            //Act
            var deserialized = (HashSet<int>)engine.Invoke(hashset, typeof(HashSet<int>));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(hashset, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void IImmutableSet_of_class(Func<object, Type, dynamic> engine)
        {
            //Arrange
            IImmutableSet<BaseTestClass> set = _fixture.Create<IEnumerable<BaseTestClass>>().ToImmutableHashSet();

            //Act
            var deserialized = (ImmutableHashSet<BaseTestClass>)engine.Invoke(set, typeof(ImmutableHashSet<BaseTestClass>));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(set, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_with_empty_list(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithConstructorPopulatingProperty testClass = new ClassWithConstructorPopulatingProperty();


            //Act
            var deserialized = (ClassWithConstructorPopulatingProperty)engine.Invoke(testClass, typeof(ClassWithConstructorPopulatingProperty));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(testClass.anotherList, deserialized.anotherList);
            Assert.Equal(testClass.nestedList, deserialized.nestedList);
            deserialized.stringProperty.Should().BeNullOrEmpty();
        }

        [Fact(Skip = "MultidimensionalArray is not supported yet")]
        public void Component_MultidimensionalArray_ResultIsTheSameAsInput()
        {
            //Arrange
            MultidimensionalArrayClass array = _fixture.Create<MultidimensionalArrayClass>();


            //Act
            var result = AvroConvert.Serialize(array);

            var deserialized = AvroConvert.Deserialize<MultidimensionalArrayClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(array, deserialized);
        }
    }
}
