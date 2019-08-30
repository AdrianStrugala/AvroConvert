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
        public void GenerateSchema_PropertiesAreDecoratedWithDefaultValueAttributes_SchemaPositionsNullTypeBeforeOtherInTheTypeArrayWhenDefaultIsNull()
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
        public void GenerateSchema_PropertiesAreDecoratedWithDefaultValueAttributes_SchemaPositionsNullTypeAfterOtherInTheTypeArrayWhenDefaultIsNotNull()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            Assert.Contains("{\"name\":\"andLongBigDefaultedProperty\",\"type\":[\"long\",\"null\"],\"default\":9200000000000000007}", schema);
        }

        [Fact]
        public void GenerateSchema_PropertiesAreDecoratedWithDefaultValueAttributes_SchemaPositionsTypesEffectivelyRegardlessOfMismatchBetweenDefaultValueAndPropertyType()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            // Assert - The DefaultValue is an int, (100)  but the property is a long, matching 
            // isn't necessary, all that's required is that the 'not null' type is first in the schema list
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

        [Fact]
        public void GenerateSchema_ClassWithMixedMembersAttributesAndNon_AfterIncludingOnlyMembersNonAttributedAreIgnored()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(MixedDataMembers), includeOnlyDataContractMembers: true);

            //Assert
            Assert.Contains("{\"type\":\"record\",\"name\":\"AvroConvertTests.MixedDataMembers\",\"fields\":[{\"name\":\"savedValues\",\"type\":{\"type\":\"array\",\"items\":\"int\"}},{\"name\":\"andAnother\",\"type\":[\"null\",\"long\"]}]}", schema);
        }
    }
}