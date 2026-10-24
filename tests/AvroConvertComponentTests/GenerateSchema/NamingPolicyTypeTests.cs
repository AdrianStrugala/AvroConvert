using AwesomeAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;
using NUnit.Framework;

namespace AvroConvertComponentTests.GenerateSchema;

public class NamingPolicyTypeTests
{
    [Test]
    public void GenerateSchema_UsingNamingPolicy_UsesCorrectTypeNames()
    {
        //Arrange
        var options = new AvroConvertOptions
        {
            NamingPolicy = new JavaAvroNamingPolicy("com.company.product")
        };

        //Act
        string schema = AvroConvert.GenerateSchema(typeof(BasicTypeWithProperties), options);

        //Assert
        schema.Should().Contain("\"name\":\"BasicTypeWithProperties\",\"namespace\":\"com.company.product\"");
        schema.Should().Contain("\"name\":\"BaseClass\",\"namespace\":\"com.company.product\"");
        schema.Should().Contain("\"name\":\"EnumWithDifferentNames\",\"namespace\":\"com.company.product\"");
    }

    [Test]
    public void GenerateSchema_UsingNamingPolicy_UseCorrectMemberNames()
    {
        //Arrange
        var options = new AvroConvertOptions
        {
            NamingPolicy = new JavaAvroNamingPolicy("com.company.product")
        };

        //Act
        string schema = AvroConvert.GenerateSchema(typeof(BasicTypeWithProperties), options);

        //Assert
        schema.Should().Contain("\"name\":\"propertyName\"");
        schema.Should().Contain("\"name\":\"enumValue\"");
    }

    [Test]
    public void GenerateSchema_UsingNamingPolicy_UseCorrectEnumSymbols()
    {
        //Arrange
        var options = new AvroConvertOptions
        {
            NamingPolicy = new JavaAvroNamingPolicy("com.company.product")
        };

        //Act
        string schema = AvroConvert.GenerateSchema(typeof(BasicTypeWithProperties), options);

        //Assert
        schema.Should().Contain("\"symbols\":[\"NONE\",\"COMBINED_OPTION\",\"OPTION_1\",\"OPTION_23\",\"OPTION_1_WITH_SUFFIX\",\"OPTION_23_WITH_SUFFIX\"]");
    }
}