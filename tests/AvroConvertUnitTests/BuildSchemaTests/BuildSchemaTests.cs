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
        var result = Schema.Create(schema.ToString());

        // Assert
        Assert.Equal(schema.ToString(), result.ToString());
    }

    [Fact]
    public void WhenTheDefaultValueForGUIDIsValid_ThenTheSchemaShouldBeCreated()
    {
        // Arrange
        var schema = Schema.Create(typeof(GuidClassWithValidDefaultValue));

        // Act
        var resultSchema = Schema.Create(schema.ToString());

        // Assert
        Assert.Equal(schema.ToString(), resultSchema.ToString());
    }

    [Fact]
    public void WhenTheDefaultValueForGuidIsInvalid_ThenAnExceptionShouldBeThrown()
    {
        // Arrange
        var schema = Schema.Create(typeof(GuidClassWithInvalidDefaultValue));

        // Act and Assert
        Assert.Throws<SerializationException>(() => Schema.Create(schema.ToString()));
    }
}

public class GuidClassWithValidDefaultValue
{
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public Guid Id { get; set; }
}

public class GuidClassWithInvalidDefaultValue
{
    [DefaultValue("invalid-guid")]
    public Guid Id { get; set; }
}