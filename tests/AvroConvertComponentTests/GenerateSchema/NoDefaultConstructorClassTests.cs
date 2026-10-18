using AwesomeAssertions;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.GenerateSchema
{
    public class NoDefaultConstructorClassTests
    {

        [Test]
        public void GenerateSchema_ClassContainsNoDefaultConstructor_SchemaIsGenerated()
        {
            //Arrange


            //Act
            string schema = AvroConvert.GenerateSchema(typeof(ClassWithoutDefaultConstructor));


            //Assert
            schema.Should().Be(@"{""name"":""ClassWithoutDefaultConstructor"",""namespace"":""AvroConvertComponentTests"",""type"":""record"",""fields"":[{""name"":""Name"",""type"":""string""}]}");
        }

    }
}
