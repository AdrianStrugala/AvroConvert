using AwesomeAssertions;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.GenerateSchema
{
    public class AttributesClassesTests
    {

        [Test]
        public void GenerateSchema_PropertiesAreDecoratedWithDefaultValueAttributes_SchemaContainsDefaultFieldForMembers()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            schema.Should().Contain("\"default\":\"Let's go\"");
            schema.Should().Contain("\"default\":9200000000000000007");
            schema.Should().Contain("\"default\":null");
        }

        [Test]
        public void GenerateSchema_PropertiesAreDecoratedWithDefaultValueAttributes_SchemaPositionsNullTypeBeforeOtherInTheTypeArrayWhenDefaultIsNull()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            schema.Should().Contain("{\"name\":\"andNullProperty\",\"type\":[\"null\",\"long\"],\"default\":null}");
        }

        [Test]
        public void GenerateSchema_PropertiesIncludeNullableVersionsOfTypes_SchemaIncludesNullTypeInTypesArray()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            schema.Should().Contain("{\"name\":\"andLongProperty\",\"type\":[\"null\",\"long\"]");
        }

        [Test]
        public void GenerateSchema_PropertiesAreDecoratedWithDefaultValueAttributes_SchemaPositionsNullTypeAfterOtherInTheTypeArrayWhenDefaultIsNotNull()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            schema.Should().Contain("{\"name\":\"andLongBigDefaultedProperty\",\"type\":[\"long\",\"null\"],\"default\":9200000000000000007}");
        }

        [Test]
        public void GenerateSchema_PropertiesAreDecoratedWithDefaultValueAttributes_SchemaPositionsTypesEffectivelyRegardlessOfMismatchBetweenDefaultValueAndPropertyType()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            // Assert - The DefaultValue is an int, (100)  but the property is a long, matching 
            // isn't necessary, all that's required is that the 'not null' type is first in the schema list
            schema.Should().Contain("{\"name\":\"andLongSmallDefaultedProperty\",\"type\":[\"long\",\"null\"],\"default\":100}");
        }

        [Test]
        public void GenerateSchema_PropertiesAreDecoratedWithDefaultValueAttributes_SchemaPositionsOriginalTypeBeforeNullWhenDefaultIsNotNull()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(DefaultValueClass));

            //Assert
            schema.Should().Contain("{\"name\":\"justSomeProperty\",\"type\":[\"string\",\"null\"],\"default\":\"Let's go\"}");
        }

        [Test]
        public void GenerateSchema_ClassWithMixedMembersAttributesAndNon_AfterIncludingOnlyMembersNonAttributedAreIgnored()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(MixedDataMembers), includeOnlyDataContractMembers: true);

            //Assert
            schema.Should().Contain("{\"name\":\"MixedDataMembers\",\"namespace\":\"AvroConvertComponentTests\",\"type\":\"record\",\"fields\":[{\"name\":\"savedValues\",\"type\":{\"type\":\"array\",\"items\":\"int\"}},{\"name\":\"andAnother\",\"type\":[\"null\",\"long\"]}]}");
        }

        [Test]
        public void GenerateSchema_Enum_SchemaIsCorrect()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(TestEnum));

            //Assert
            schema.Should().Contain("{\"type\":\"enum\",\"name\":\"TestEnum\",\"namespace\":\"AvroConvertComponentTests\",\"symbols\":[\"a\",\"be\",\"ca\",\"dlo\"]}");
        }

        [Test]
        public void GenerateSchema_PropertiesAreDecoratedWithNullableSchemaAttribute_SchemaIndicatesFieldIsNullable()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(AttributeClass));

            // Assert
            schema.Should().Contain("{\"name\":\"favorite_number\",\"aliases\":[\"NullableIntProperty\"],\"type\":[\"null\",\"int\"]}");
        }

        [Test]
        public void GenerateSchema_PropertiesAreSystemNullable_SchemaIndicatesFieldIsNullable()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(User));

            // Assert
            schema.Should().Contain("{\"name\":\"favorite_number\",\"type\":[\"null\",\"int\"]}");
        }

        [Test]
        public void GenerateSchema_PropertiesAreNullableReferenceTypes_SchemaIndicatesMembersAreNullable()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(ClassWithNullableMembers));

            // Assert
            schema.Should().Contain("{\"name\":\"NullableStringProperty\",\"type\":[\"null\",\"string\"]}");
            schema.Should().Contain("{\"name\":\"NullableField\",\"type\":[\"null\",\"string\"]}");
            schema.Should().Contain("{\"name\":\"NullableRecord\",\"type\":[\"null\",{\"name\":\"UserNameClass\",\"namespace\":\"AvroConvertComponentTests\",\"type\":\"record\",\"fields\":[{\"name\":\"name\",\"type\":\"string\"}]}]}");
        }

        [Test]
        public void GenerateSchema_PropertiesWithDuplicateTypes_EmitsSingleRecord()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GenerateSchema(typeof(ClassWithDuplicateTypes));

            // Assert
            schema.Should().Contain("{\"name\":\"Property2\",\"type\":\"AvroConvertComponentTests.BaseClass\"}");
        }
    }
}
