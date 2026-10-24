using AwesomeAssertions;
using SolTechnology.Avro.Policies;
using NUnit.Framework;

namespace AvroConvertComponentTests.Policies;

public class JavaAvroNamingPolicyTests
{
    [Test]
    public void Policy_Type_GetsValidName()
    {
        //Arrange
        var policy = new JavaAvroNamingPolicy("com.company.product");

        //Act
        var info = policy.GetTypeName(typeof(DifferentCaseBaseTestClass));

        //Assert
        info.Name.Should().Be(nameof(DifferentCaseBaseTestClass));
        info.Namespace.Should().Be("com.company.product");
    }

    [Test]
    public void Policy_TypeMember_GetsValidName()
    { 
        //Arrange
        var policy = new JavaAvroNamingPolicy("com.company.product");
        var member = typeof(DifferentCaseBaseTestClass).GetProperty(nameof(DifferentCaseBaseTestClass.ObjectProperty));

        //Act
        var name = policy.GetMemberName(member);

        //Assert
        name.Should().Be("objectProperty");
    }

    [TestCase(nameof(EnumWithDifferentNames.CombinedOption), "COMBINED_OPTION")]
    [TestCase(nameof(EnumWithDifferentNames.None), "NONE")]
    [TestCase(nameof(EnumWithDifferentNames.Option1), "OPTION_1")]
    [TestCase(nameof(EnumWithDifferentNames.Option23), "OPTION_23")]
    [TestCase(nameof(EnumWithDifferentNames.Option1WithSuffix), "OPTION_1_WITH_SUFFIX")]
    [TestCase(nameof(EnumWithDifferentNames.Option23WithSuffix), "OPTION_23_WITH_SUFFIX")]
    public void Policy_EnumMember_GetsValidName(string symbol, string expected)
    {
        //Arrange
        var policy = new JavaAvroNamingPolicy("com.company.product");
        var member = typeof(EnumWithDifferentNames).GetField(symbol);

        //Act
        var name = policy.GetMemberName(member);

        //Assert
        name.Should().Be(expected);
    }
}