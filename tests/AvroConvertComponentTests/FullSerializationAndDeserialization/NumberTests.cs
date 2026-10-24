using AwesomeAssertions;
using System;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization;

public class NumberTests
{
    [Test]
    public void Class_WithTooBigDecimal_ShouldThrow_ByDefault([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
    {
        //Arrange
        var record = new LogicalTypesClass();
        record.One = 1.12345678901234567890m; // Default scale is 14

        //Act
        var exception = Recorded.Exception(() =>RoundTrip.Run<LogicalTypesClass>(record, format));

        //Assert
        exception.Should().NotBeNull();
    }

    [Test]
    public void Class_WithTooBigDecimal_ShouldTruncateOrRound_UsingHandling([Values(AvroNumberHandling.Truncate, AvroNumberHandling.Rounding)] AvroNumberHandling numberHandling)
    {
        //Arrange
        var record = new LogicalTypesClass();
        record.One = 1.12345678901234567890m; // Default scale is 14

        //Act
        var deserialized = RoundTrip.Run<LogicalTypesClass>(record, Format.Headless, new AvroConvertOptions { NumberHandling = numberHandling });
        var value = (decimal)deserialized.One;

        //Assert
        deserialized.Should().NotBeNull();
        GetScale(value).Should().Be(14);
        (value >= 1.12345678901234m && value <= 1.12345678901235m).Should().BeTrue();
    }

    private int GetScale(decimal value)
    {
        if (value == 0)
        {
            return 0;
        }

        var bits = decimal.GetBits(value);

        return (bits[3] >> 16) & 0x7F;
    }
}