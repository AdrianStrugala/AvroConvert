using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AutoFixture;
using AwesomeAssertions;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class CollectionsTests
    {
        private readonly Fixture _fixture = new();

        [Test]
        public void List_of_class([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            var someTestClasses = _fixture.CreateMany<BaseTestClass>().ToList();

            //Act
            var deserialized = RoundTrip.Run<List<BaseTestClass>>(someTestClasses, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(someTestClasses, o => o.WithStrictOrdering());
        }

        [Test]
        public void List_with_single_item([ValueSource(typeof(Formats), nameof(Formats.ContainerOnly))] Format format)
        {
            //Arrange
            var oneItem = new List<BaseTestClass> { _fixture.Create<BaseTestClass>() };

            //Act
            var deserialized = RoundTrip.Run<List<BaseTestClass>>(oneItem, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(oneItem, o => o.WithStrictOrdering());
        }

        [Test]
        [Property("Fix", "https://github.com/AdrianStrugala/AvroConvert/issues/146")]
        public void IEnumerable_of_class([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            var someTestClasses = _fixture.CreateMany<BaseTestClass>();

            //Act
            var deserialized = RoundTrip.Run<IEnumerable<BaseTestClass>>(someTestClasses, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(someTestClasses, o => o.WithStrictOrdering());
        }

        [Test]
        public void Class_with_list([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithSimpleList testClass = _fixture.Create<ClassWithSimpleList>();

            //Act
            var deserialized = RoundTrip.Run<ClassWithSimpleList>(testClass, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Test]
        public void Class_with_list_of_classes([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithConstructorPopulatingProperty
                testClass = _fixture.Create<ClassWithConstructorPopulatingProperty>();

            //Act
            var deserialized = RoundTrip.Run<ClassWithConstructorPopulatingProperty>(testClass, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(testClass);

        }

        [Test]
        public void Class_with_array([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithArray
                testClass = _fixture.Create<ClassWithArray>();

            //Act
            var deserialized = RoundTrip.Run<ClassWithArray>(testClass, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Test]
        public void List_of_int([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            List<int> toSerialize = _fixture.Create<List<int>>();

            //Act
            var deserialized = RoundTrip.Run<List<int>>(toSerialize, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(deserialized);
        }

        [Test]
        public void Array_of_int([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            int[] array = _fixture.Create<int[]>();

            //Act
            var deserialized = RoundTrip.Run<List<int>>(array, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().Equal(array);
        }

        [Test]
        public void HashSet_of_int([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            HashSet<int> hashset = _fixture.Create<HashSet<int>>();

            //Act
            var deserialized = RoundTrip.Run<HashSet<int>>(hashset, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(hashset);
        }

        [Test]
        public void IImmutableSet_of_class([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            IImmutableSet<BaseTestClass> set = _fixture.Create<IEnumerable<BaseTestClass>>().ToImmutableHashSet();

            //Act
            var deserialized = RoundTrip.Run<ImmutableHashSet<BaseTestClass>>(set, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(set);
        }

        [Test]
        public void Class_with_empty_list([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithConstructorPopulatingProperty testClass = new ClassWithConstructorPopulatingProperty();


            //Act
            var deserialized = RoundTrip.Run<ClassWithConstructorPopulatingProperty>(testClass, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.anotherList.Should().BeEquivalentTo(testClass.anotherList, o => o.WithStrictOrdering());
            deserialized.nestedList.Should().BeEquivalentTo(testClass.nestedList, o => o.WithStrictOrdering());
            deserialized.stringProperty.Should().BeNullOrEmpty();
        }
    }
}
