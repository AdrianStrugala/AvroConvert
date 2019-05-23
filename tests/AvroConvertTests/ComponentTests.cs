namespace AvroConvertTests
{
    using AutoFixture;
    using System.Collections.Generic;
    using AvroConvert;
    using Xunit;

    [Collection("MemoryBasedTests")]
    public class ComponentTests
    {
        private readonly Fixture _fixture;

        public ComponentTests()
        {
            _fixture = new Fixture();
        }
        [Fact]
        public void Serialize_ThenDeserialize_ObjectsAreEqual()
        {
            //Arrange
            User user = new User();
            user.name = "Krzys";
            user.favorite_color = "yellow";
            user.favorite_number = null;

            //Act
            var serialized = AvroConvert.Serialize(user);

            var deserialized = AvroConvert.Deserialize<User>(serialized);

            //Assert
            Assert.NotNull(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal(user.name, deserialized.name);
            Assert.Equal(user.favorite_color, deserialized.favorite_color);
            Assert.Equal(user.favorite_number, deserialized.favorite_number);
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
        public void Serialize_ObjectContainsLists_ResultIsTheSameAsInput()
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
        public void Serialize_ObjectContainsComplexLists_ResultIsTheSameAsInput()
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
        public void Serialize_ObjectContainsArray_ResultIsTheSameAsInput()
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
        public void Serialize_ObjectContainsGuid_ResultIsTheSameAsInput()
        {
            //Arrange
            ClassWithGuid
                testClass = _fixture.Create<ClassWithGuid>();

            //Act

            var result = AvroConvert.Serialize(testClass);

            var deserialized = AvroConvert.Deserialize<ClassWithGuid>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testClass.theGuid, deserialized.theGuid);
        }

        [Fact]
        public void Serialize_ObjectIsList_ResultIsTheSameAsInput()
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
        public void Serialize_ObjectIsDictionary_ResultIsTheSameAsInput()
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
        public void Serialize_ObjectIsArray_ResultIsTheSameAsInput()
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
        public void Serialize_ObjectIsAvroMap_ResultIsTheSameAsInput()
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
    }
}
