using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.GenerateSchema
{
    public class NoDefaultConstructorClassTests
    {

        [Fact]
        public void GenerateSchema_ClassContainsNoDefaultConstructor_SchemaIsGenerated()
        {
            //Arrange


            //Act
            string schema = AvroConvert.GenerateSchema(typeof(ClassWithoutDefaultConstructor));


            //Assert
            Assert.Equal(@"{""type"":""record"",""name"":""ClassWithoutDefaultConstructor"",""namespace"":""AvroConvertComponentTests"",""fields"":[{""name"":""Name"",""type"":""string""}]}", schema);
        }

    }
}
