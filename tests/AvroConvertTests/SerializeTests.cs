namespace AvroConvertTests
{
    using AutoFixture;
    using System.Collections.Generic;
    using AvroConvert;
    using Xunit;

    [Collection("MemoryBasedTests")]
    public class SerializeTests
    {
        private readonly Fixture _fixture;

        public SerializeTests()
        {
            _fixture = new Fixture();
        }
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
            var result = AvroConvert.Serialize(someTestClasses);

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
            var result = AvroConvert.Serialize(someTestClasses);

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
            var result = AvroConvert.Serialize(user);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Serialize_ClassHasConstructorFillingProperty_NoExceptionIsThrown()
        {
            //Arrange
            ClassWithConstructorPopulatingProperty testClass = new ClassWithConstructorPopulatingProperty();

            //Act
            var result = AvroConvert.Serialize(testClass);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Serialize_VeryComplexClass_NoExceptionIsThrown()
        {
            //Arrange
            VeryComplexClass
                testClass =
                    _fixture.Create<VeryComplexClass>();

            //Act
            var result = AvroConvert.Serialize(testClass);


            //Assert
            Assert.NotNull(result);
        }
    }
}
