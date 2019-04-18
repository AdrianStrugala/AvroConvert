namespace AvroConvertTests
{
    using AutoFixture;
    using Avro;
    using System.Collections.Generic;
    using Xunit;

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

            var deserialized = global::Avro.AvroConvert.Deserialize<User>(serialized);

            //Assert
            Assert.NotNull(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal(user.name, deserialized.name);
            Assert.Equal(user.favorite_color, deserialized.favorite_color);
            Assert.Equal(user.favorite_number, deserialized.favorite_number);
        }

        [Fact]
        public void SerializeList_ThenDeserialize_ListsAreEqual()
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
            var serialized = global::Avro.AvroConvert.Serialize(someTestClasses);
            var deserialized = global::Avro.AvroConvert.Deserialize<List<SomeTestClass>>(serialized);

            //Assert
            Assert.NotNull(deserialized);
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
        }
    }
}
