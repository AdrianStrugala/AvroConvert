using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.DefaultSerializationDeserialization
{
    public class ClassesWithoutDefaultConstructorTests
    {
        private readonly Fixture _fixture;

        public ClassesWithoutDefaultConstructorTests()
        {
            _fixture = new AutoFixture.Fixture();
        }


        [Fact]
        public void Serialize_ClassWithoutDefaultConstructor_NoExceptionIsThrown()
        {
            //Arrange
            ClassWithoutDefaultConstructor testClass =
                _fixture.Create<ClassWithoutDefaultConstructor>();


            //Act
            var serialized = AvroConvert.Serialize(testClass);
            var deserialized = AvroConvert.Deserialize<ClassWithoutDefaultConstructor>(serialized);


            //Assert
            deserialized.Should().BeEquivalentTo(testClass);
        }

        [Fact]
        public void Serialize_ComplexClassWithoutDefaultConstructor_NoExceptionIsThrown()
        {
            //Arrange
            VeryComplexClassWithoutDefaultConstructor testClass =
                _fixture.Create<VeryComplexClassWithoutDefaultConstructor>();


            //Act
            var serialized = AvroConvert.Serialize(testClass);
            var deserialized = AvroConvert.Deserialize<VeryComplexClassWithoutDefaultConstructor>(serialized);


            //Assert
            deserialized.Should().BeEquivalentTo(testClass);
        }
    }
}
