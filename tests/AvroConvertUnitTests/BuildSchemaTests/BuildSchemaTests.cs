using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using Xunit;

namespace AvroConvertUnitTests.BuildSchemaTests;

public sealed class BuildSchemaTests
{
    [Fact]
    public void BuildSchema_JsonSchemaContainsAvroAttributes_ResultIsEqualToInput()
    {
        // Arrange
        var schema = Schema.Create(typeof(AttributeClass));

        // Act
        var result = Schema.Parse(schema.ToString());

        // Assert
        Assert.Equal(schema.ToString(), result.ToString());
    }

    [Fact]
    public void WhenTheDefaultValueForGUIDIsValid_ThenTheSchemaShouldBeCreated()
    {
        // Arrange
        var schema = Schema.Create(typeof(GuidClassWithValidDefaultValue));

        // Act
        var resultSchema = Schema.Parse(schema.ToString());

        // Assert
        Assert.Equal(schema.ToString(), resultSchema.ToString());
    }

    [Fact]
    public void WhenTheDefaultValueForGuidIsInvalid_ThenAnExceptionShouldBeThrown()
    {
        // Arrange
        var schema = Schema.Create(typeof(GuidClassWithInvalidDefaultValue));

        // Act and Assert
        Assert.Throws<SerializationException>(() => Schema.Parse(schema.ToString()));
    }
}

public class GuidClassWithValidDefaultValue
{
    [DefaultValue("dec57724-9fa9-4062-b5a4-e6c41c71e585")]
    public Guid Id { get; set; }
}

public class GuidClassWithInvalidDefaultValue
{
    [DefaultValue("invalid-guid")]
    public Guid Id { get; set; }
}