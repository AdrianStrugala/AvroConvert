using SolTechnology.Avro.Policies;
using Xunit;

namespace AvroConvertComponentTests.Policies;

public class JavaAvroNamingPolicyTests
{
    [Fact]
    public void Policy_Type_GetsValidName()
    {
        //Arrange
        var policy = new JavaAvroNamingPolicy("com.company.product");

        //Act
        var info = policy.GetTypeName(typeof(DifferentCaseBaseTestClass));

        //Assert
        Assert.Equal(nameof(DifferentCaseBaseTestClass), info.Name);
        Assert.Equal("com.company.product", info.Namespace);
    }

    [Fact]
    public void Policy_TypeMember_GetsValidName()
    { 
        //Arrange
        var policy = new JavaAvroNamingPolicy("com.company.product");
        var member = typeof(DifferentCaseBaseTestClass).GetProperty(nameof(DifferentCaseBaseTestClass.ObjectProperty));

        //Act
        var name = policy.GetMemberName(member);

        //Assert
        Assert.Equal("objectProperty", name);
    }

    [Theory]
    [InlineData(nameof(EnumWithDifferentNames.CombinedOption), "COMBINED_OPTION")]
    [InlineData(nameof(EnumWithDifferentNames.None), "NONE")]
    [InlineData(nameof(EnumWithDifferentNames.Option1), "OPTION_1")]
    [InlineData(nameof(EnumWithDifferentNames.Option23), "OPTION_23")]
    [InlineData(nameof(EnumWithDifferentNames.Option1WithSuffix), "OPTION_1_WITH_SUFFIX")]
    [InlineData(nameof(EnumWithDifferentNames.Option23WithSuffix), "OPTION_23_WITH_SUFFIX")]
    public void Policy_EnumMember_GetsValidName(string symbol, string expected)
    {
        //Arrange
        var policy = new JavaAvroNamingPolicy("com.company.product");
        var member = typeof(EnumWithDifferentNames).GetField(symbol);

        //Act
        var name = policy.GetMemberName(member);

        //Assert
        Assert.Equal(expected, name);
    }
}