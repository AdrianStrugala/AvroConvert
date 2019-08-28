namespace AvroConvertTests.Component
{
    using System.Collections.Generic;
    using AutoFixture;
    using AvroConvert;
    using Xunit;

    public class ListClassTests
    {
        private readonly Fixture _fixture;

        public ListClassTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void SerializeClassWithList_ThenDeserialize_ListsAreEqual()
        {
            //Arrange
            SomeTestClass someTestClass = new SomeTestClass
            {
                objectProperty = new NestedTestClass
                {
                    justSomeProperty = "spoko",
                    andLongProperty = 2137
                },
                simpleProperty = 111111
            };

            SomeTestClass dupa2 = new SomeTestClass
            {
                objectProperty = new NestedTestClass
                {
                    justSomeProperty = "loko",
                    andLongProperty = 2137
                },
                simpleProperty = 2135
            };

            SomeTestClass[] someTestClasses = new SomeTestClass[2];
            someTestClasses[0] = someTestClass;
            someTestClasses[1] = dupa2;

            //Act
            var serialized = AvroConvert.Serialize(someTestClasses);
            var deserialized = AvroConvert.Deserialize<List<SomeTestClass>>(serialized);

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(someTestClasses, deserialized);
        }

        [Fact]
        public void Component_ObjectContainsLists_ResultIsTheSameAsInput()
        {
            //Arrange
            ClassWithSimpleList
                testClass = _fixture.Create<ClassWithSimpleList>();

            //Act

            var result = AvroConvert.Serialize(testClass);

            var deserialized = AvroConvert.Deserialize<ClassWithSimpleList>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testClass.someList.Count, deserialized.someList.Count);
        }

        [Fact]
        public void Component_ObjectContainsComplexLists_ResultIsTheSameAsInput()
        {
            //Arrange
            ClassWithConstructorPopulatingProperty
                testClass = _fixture.Create<ClassWithConstructorPopulatingProperty>();

            //Act

            var result = AvroConvert.Serialize(testClass);

            var deserialized = AvroConvert.Deserialize<ClassWithConstructorPopulatingProperty>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testClass.nestedList.Count, deserialized.nestedList.Count);
            Assert.Equal(testClass.stringProperty, deserialized.stringProperty);
            Assert.Equal(testClass, deserialized);

        }

        [Fact]
        public void Component_ObjectContainsArray_ResultIsTheSameAsInput()
        {
            //Arrange
            ClassWithArray
                testClass = _fixture.Create<ClassWithArray>();

            //Act

            var result = AvroConvert.Serialize(testClass);

            var deserialized = AvroConvert.Deserialize<ClassWithArray>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testClass.theArray.Length, deserialized.theArray.Length);
        }

        [Fact]
        public void Component_ObjectIsList_ResultIsTheSameAsInput()
        {
            //Arrange
            List<int> dictionary = _fixture.Create<List<int>>();

            //Act

            var result = AvroConvert.Serialize(dictionary);

            var deserialized = AvroConvert.Deserialize<List<int>>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(dictionary, deserialized);
        }

        [Fact]
        public void Component_ObjectIsArray_ResultIsTheSameAsInput()
        {
            //Arrange
            int[] array = _fixture.Create<int[]>();

            //Act

            var result = AvroConvert.Serialize(array);

            var deserialized = AvroConvert.Deserialize<int[]>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(array, deserialized);
        }

        [Fact]
        public void Component_ObjectIsHashSet_ResultIsTheSameAsInput()
        {
            //Arrange
            HashSet<int> hashset = _fixture.Create<HashSet<int>>();

            //Act

            var result = AvroConvert.Serialize(hashset);

            var deserialized = AvroConvert.Deserialize<HashSet<int>>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(hashset, deserialized);
        }

        [Fact (Skip = "MultidimensionalArray is not supported yet")]
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
