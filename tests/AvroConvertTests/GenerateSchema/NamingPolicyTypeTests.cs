using SolTechnology.Avro;
using SolTechnology.Avro.Policies;
using Xunit;

namespace AvroConvertComponentTests.GenerateSchema;

public class NamingPolicyTypeTests
{
    [Fact]
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
        Assert.Contains("\"name\":\"BasicTypeWithProperties\",\"namespace\":\"com.company.product\"", schema);
        Assert.Contains("\"name\":\"BaseClass\",\"namespace\":\"com.company.product\"", schema);
        Assert.Contains("\"name\":\"EnumWithDifferentNames\",\"namespace\":\"com.company.product\"", schema);
    }

    [Fact]
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
        Assert.Contains("\"name\":\"propertyName\"", schema);
        Assert.Contains("\"name\":\"enumValue\"", schema);
    }

    [Fact]
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
        Assert.Contains("\"symbols\":[\"NONE\",\"COMBINED_OPTION\",\"OPTION_1\",\"OPTION_23\",\"OPTION_1_WITH_SUFFIX\",\"OPTION_23_WITH_SUFFIX\"]", schema);
    }
}