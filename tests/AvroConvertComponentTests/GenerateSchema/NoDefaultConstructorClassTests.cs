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
            Assert.Equal(@"{""name"":""ClassWithoutDefaultConstructor"",""namespace"":""AvroConvertComponentTests"",""type"":""record"",""fields"":[{""name"":""Name"",""type"":""string""}]}", schema);
        }

    }
}
