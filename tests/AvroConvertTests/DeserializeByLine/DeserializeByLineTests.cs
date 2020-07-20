using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertTests.DeserializeByLine
{
    public class DeserializeByLineTests
    {
        private readonly Fixture _fixture;
        private readonly byte[] _avroBytes = System.IO.File.ReadAllBytes("example2.avro");

        public DeserializeByLineTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Deserialize_BlockData_ItIsCorrectlyDeserialized()
        {
            //Arrange
            var expectedResult = new List<User>();
            expectedResult.Add(new User
            {
                name = "Alyssa",
                favorite_number = 256,
                favorite_color = null
            });

            expectedResult.Add(new User
            {
                name = "Ben",
                favorite_number = 7,
                favorite_color = "red"
            });

            //Act
            var result = new List<User>();
            using (var reader = AvroConvert.OpenDeserializer<User>(new MemoryStream(_avroBytes)))
            {
                while (reader.HasNext())
                {
                    var item = reader.ReadNext();

                    result.Add(item);
                }
            }


            //Assert
            Assert.Equal(expectedResult, result);
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

            //            File.WriteAllBytes("x", serialized);

            var xd = AvroConvert.Deserialize<SomeTestClass[]>(serialized);

            var result = new List<SomeTestClass>();

            using (var reader = AvroConvert.OpenDeserializer<SomeTestClass>(new MemoryStream(serialized)))
            {
                while (reader.HasNext())
                {
                    var item = reader.ReadNext();

                    result.Add(item);
                }
            }


            //Assert
            Assert.NotNull(result);
            Assert.Equal(someTestClasses, result);
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

        [Fact]
        public void Component_ObjectIsIImmutableSet_ResultIsTheSameAsInput()
        {
            //Arrange
            IImmutableSet<SomeTestClass> set = _fixture.Create<IEnumerable<SomeTestClass>>().ToImmutableHashSet();

            //Act

            var result = AvroConvert.Serialize(set);

            var deserialized = AvroConvert.Deserialize<ImmutableHashSet<SomeTestClass>>(result);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(set, deserialized);
        }

        [Fact]
        public void Component_ObjectContainsEmptyList_ResultIsTheSameAsInput()
        {
            //Arrange
            ClassWithConstructorPopulatingProperty testClass = new ClassWithConstructorPopulatingProperty();


            //Act
            var result = AvroConvert.Serialize(testClass);

            var deserialized = AvroConvert.Deserialize<ClassWithConstructorPopulatingProperty>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(testClass.anotherList, deserialized.anotherList);
            Assert.Equal(testClass.nestedList, deserialized.nestedList);
            Assert.True(Comparison.AreEqual(testClass.stringProperty, deserialized.stringProperty));
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
