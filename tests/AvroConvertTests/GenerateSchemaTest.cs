namespace AvroConvertTests
{
    using AvroConvert;
    using Xunit;

    public class GenerateSchemaTest
    {

        [Fact]
        public void GenerateSchema_PropertiesAreDecoratedWithDefaultValueAttributes_SchemaContainsDefaultFieldForMembers()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            Assert.Contains("\"default\":\"Let's go\"", schema);
            Assert.Contains("\"default\":\"2137\"", schema);
        }
    }
}
