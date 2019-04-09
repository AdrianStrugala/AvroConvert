namespace AvroConvertTests
{
    using System.Collections.Generic;
    using Xunit;

    public class ComponentTests
    {
        [Fact]
        public void Serialize_ThenDeserialize_ObjectsAreEqual()
        {
            //Arrange
            User user = new User();
            user.name = "Krzys";
            user.favorite_color = "yellow";
            user.favorite_number = null;

            //Act
            var serialized = AvroConvert.AvroConvert.Serialize(user);

            var deserialized = AvroConvert.AvroConvert.Deserialize<User>(serialized);

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
            var serialized = AvroConvert.AvroConvert.Serialize(someTestClasses);
            var deserialized = AvroConvert.AvroConvert.Deserialize<List<SomeTestClass>>(serialized);

            //Assert
            Assert.NotNull(deserialized);
        }
    }
}
