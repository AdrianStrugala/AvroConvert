using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests
{
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
            List<BaseTestClass> someTestClasses = _fixture.CreateMany<BaseTestClass>().ToList();

            //Act
            var result = AvroConvert.Serialize(someTestClasses);

            //Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Serialize_InputIsArray_NoExceptionIsThrown()
        {
            //Arrange
            BaseTestClass[] someTestClasses = _fixture.CreateMany<BaseTestClass>().ToArray();

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

        [Fact]
        public void Serialize_ClassWithEnum_NoExceptionIsThrown()
        {
            //Arrange
            ClassWithEnum testClass = _fixture.Create<ClassWithEnum>();

            //Act
            var result = AvroConvert.Serialize(testClass);


            //Assert
            Assert.NotNull(result);
        }
    }
}
