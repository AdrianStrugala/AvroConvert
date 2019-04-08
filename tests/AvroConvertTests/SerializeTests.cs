namespace AvroConvertTests
{
    using System.Collections.Generic;
    using Xunit;

    public class SerializeTests
    {
        [Fact]
        public void Serialize_InputIsList_NoExceptionIsThrown()
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

            SomeTestClass someTestClass2 = new SomeTestClass
            {
                objectProperty = new NestedTestClass
                {
                    justSomeProperty = "loko",
                    andLongProperty = 2137
                },
                simpleProperty = 2135
            };

            List<SomeTestClass> someTestClasses = new List<SomeTestClass>();
            someTestClasses.Add(someTestClass);
            someTestClasses.Add(someTestClass2);

            //Act
            var result = AvroConvert.AvroConvert.Serialize(someTestClasses);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Serialize_InputIsArray_NoExceptionIsThrown()
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
            var result = AvroConvert.AvroConvert.Serialize(someTestClasses);

            //Assert
            Assert.NotNull(result);
        }


        [Fact]
        public void Serialize_InputIsObject_NoExceptionIsThrown()
        {
            //Arrange
            User user = new User();
            user.name = "Krzys";
            user.favorite_color = "yellow";
            user.favorite_number = null;

            //Act
            var result = AvroConvert.AvroConvert.Serialize(user);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Serialize_SomeOneDefinedAvroAttributes_DefinedAreTaken()
        {

        }
    }
}
