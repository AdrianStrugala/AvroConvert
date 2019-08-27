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
            Assert.Contains("\"default\":9200000000000000007", schema);
            Assert.Contains("\"default\":null", schema);
        }


        [Fact]
        public void GenerateSchema_PropertiesAreDecoratedWithDefaultValueAttributes_SchemaPositionsNullTypeBeforeOtherWhenDefaultIsNull()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            Assert.Contains("{\"name\":\"andNullProperty\",\"type\":[\"null\",\"long\"],\"default\":null}", schema);
        }
        [Fact]
        public void GenerateSchema_PropertiesIncludeNullableVersionsOfTypes_SchemaIncludesNullTypeInTypesArray()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            Assert.Contains("{\"name\":\"andLongProperty\",\"type\":[\"null\",\"long\"]", schema);
        }
        
        [Fact]
        public void JOEY_TypePerfectMatchCastableMakesNullMoveToSecondPlace()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            Assert.Contains("{\"name\":\"andLongBigDefaultedProperty\",\"type\":[\"long\",\"null\"],\"default\":9200000000000000007}", schema);
        }
        [Fact]
        public void SALLY_TypeCastableMakesNullMoveToSecondPlace_notJustForPerfectMatches()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            Assert.Contains("{\"name\":\"andLongSmallDefaultedProperty\",\"type\":[\"long\",\"null\"],\"default\":100}", schema);
        }
        [Fact]
        public void GenerateSchema_PropertiesAreDecoratedWithDefaultValueAttributes_SchemaPositionsOriginalTypeBeforeNullWhenDefaultIsNotNull()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            Assert.Contains("{\"name\":\"justSomeProperty\",\"type\":[\"string\",\"null\"],\"default\":\"Let's go\"}", schema);
        }
    }
}
//"{\"name\":\"andNullProperty\",\"type\":[\"null\",\"long\"],\"default\":null}"