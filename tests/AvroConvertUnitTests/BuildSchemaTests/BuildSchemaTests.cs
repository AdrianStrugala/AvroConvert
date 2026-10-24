using AwesomeAssertions;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using NUnit.Framework;

namespace AvroConvertUnitTests.BuildSchemaTests;

public sealed class BuildSchemaTests
{
    [Test]
    public void BuildSchema_JsonSchemaContainsAvroAttributes_ResultIsEqualToInput()
    {
        // Arrange
        var schema = Schema.Create(typeof(AttributeClass));

        // Act
        var result = Schema.Parse(schema.ToString());

        // Assert
        result.ToString().Should().Be(schema.ToString());
    }

    [Test]
    public void WhenTheDefaultValueForGUIDIsValid_ThenTheSchemaShouldBeCreated()
    {
        // Arrange
        var schema = Schema.Create(typeof(GuidClassWithValidDefaultValue));

        // Act
        var resultSchema = Schema.Parse(schema.ToString());

        // Assert
        resultSchema.ToString().Should().Be(schema.ToString());
    }

    [Test]
    public void WhenTheDefaultValueForGuidIsInvalid_ThenAnExceptionShouldBeThrown()
    {
        // Arrange
        var schema = Schema.Create(typeof(GuidClassWithInvalidDefaultValue));

        // Act and Assert
        FluentActions.Invoking(() => Schema.Parse(schema.ToString())).Should().ThrowExactly<SerializationException>();
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