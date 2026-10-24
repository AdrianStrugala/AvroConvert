using AwesomeAssertions;
using Avro;
using Avro.Generic;
using NUnit.Framework;

namespace AvroConvertInteropTests;

internal static class InteropAssert
{
    /// <summary>Compares an AvroConvert-side object with what Apache.Avro read back.</summary>
    internal static void Equivalent(InteropRecord expected, GenericRecord actual)
    {
        actual[nameof(expected.IntValue)].Should().Be(expected.IntValue);
        actual[nameof(expected.LongValue)].Should().Be(expected.LongValue);
        actual[nameof(expected.FloatValue)].Should().Be(expected.FloatValue);
        actual[nameof(expected.DoubleValue)].Should().Be(expected.DoubleValue);
        actual[nameof(expected.BoolValue)].Should().Be(expected.BoolValue);
        actual[nameof(expected.StringValue)].Should().Be(expected.StringValue);
        ((byte[])actual[nameof(expected.BytesValue)]).Should().Equal(expected.BytesValue);
        ((object[])actual[nameof(expected.IntList)]).Cast<int>().Should().Equal(expected.IntList);
        ((IDictionary<string, object>)actual[nameof(expected.StringMap)]).ToDictionary(kv => kv.Key, kv => (string)kv.Value).Should().BeEquivalentTo(expected.StringMap);

        var child = (GenericRecord)actual[nameof(expected.Child)];
        child[nameof(expected.Child.Name)].Should().Be(expected.Child.Name);
        child[nameof(expected.Child.Amount)].Should().Be(expected.Child.Amount);

        ((GenericEnum)actual[nameof(expected.Color)]).Value.Should().Be(expected.Color.ToString());
        ((int?)actual[nameof(expected.NullableInt)]).Should().Be(expected.NullableInt);
        ((decimal)(AvroDecimal)actual[nameof(expected.DecimalValue)]).Should().Be(expected.DecimalValue);

        var timestamp = (DateTime)actual[nameof(expected.Timestamp)];
        timestamp.Kind.Should().Be(DateTimeKind.Utc);
        timestamp.Ticks.Should().Be(expected.Timestamp.Ticks);

        ((Guid)actual[nameof(expected.Id)]).Should().Be(expected.Id);
        DateOnly.FromDateTime((DateTime)actual[nameof(expected.Date)]).Should().Be(expected.Date);
        ((TimeSpan)actual[nameof(expected.Time)]).Should().Be(expected.Time.ToTimeSpan());
    }

    internal static void Equivalent(InteropRecord expected, InteropRecord actual)
    {
        actual.IntValue.Should().Be(expected.IntValue);
        actual.LongValue.Should().Be(expected.LongValue);
        actual.FloatValue.Should().Be(expected.FloatValue);
        actual.DoubleValue.Should().Be(expected.DoubleValue);
        actual.BoolValue.Should().Be(expected.BoolValue);
        actual.StringValue.Should().Be(expected.StringValue);
        actual.BytesValue.Should().Equal(expected.BytesValue);
        actual.IntList.Should().Equal(expected.IntList);
        actual.StringMap.Should().BeEquivalentTo(expected.StringMap);
        actual.Child.Name.Should().Be(expected.Child.Name);
        actual.Child.Amount.Should().Be(expected.Child.Amount);
        actual.Color.Should().Be(expected.Color);
        actual.NullableInt.Should().Be(expected.NullableInt);
        actual.DecimalValue.Should().Be(expected.DecimalValue);
        actual.Timestamp.Ticks.Should().Be(expected.Timestamp.Ticks);
        actual.Id.Should().Be(expected.Id);
        actual.Date.Should().Be(expected.Date);
        actual.Time.Should().Be(expected.Time);
    }
}
