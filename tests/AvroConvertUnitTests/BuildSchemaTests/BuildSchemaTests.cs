using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using Xunit;

namespace AvroConvertUnitTests.BuildSchemaTests
{
    public class BuildSchemaTests
    {
        [Fact]
        public void BuildSchema_JsonSchemaContainsAvroAttributes_ResultIsEqualToInput()
        {
            //Arrange
            var schema = Schema.Create(typeof(AttributeClass));

            //Act
            var result = Schema.Create(schema.ToString());

            //Assert
            Assert.Equal(schema.ToString(), result.ToString());
        }
    }
}
