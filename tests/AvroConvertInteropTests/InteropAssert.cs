using Avro;
using Avro.Generic;
using Xunit;

namespace AvroConvertInteropTests;

internal static class InteropAssert
{
    /// <summary>Compares an AvroConvert-side object with what Apache.Avro read back.</summary>
    internal static void Equivalent(InteropRecord expected, GenericRecord actual)
    {
        Assert.Equal(expected.IntValue, actual[nameof(expected.IntValue)]);
        Assert.Equal(expected.LongValue, actual[nameof(expected.LongValue)]);
        Assert.Equal(expected.FloatValue, actual[nameof(expected.FloatValue)]);
        Assert.Equal(expected.DoubleValue, actual[nameof(expected.DoubleValue)]);
        Assert.Equal(expected.BoolValue, actual[nameof(expected.BoolValue)]);
        Assert.Equal(expected.StringValue, actual[nameof(expected.StringValue)]);
        Assert.Equal(expected.BytesValue, (byte[])actual[nameof(expected.BytesValue)]);
        Assert.Equal(expected.IntList, ((object[])actual[nameof(expected.IntList)]).Cast<int>());
        Assert.Equal(
            expected.StringMap,
            ((IDictionary<string, object>)actual[nameof(expected.StringMap)]).ToDictionary(kv => kv.Key, kv => (string)kv.Value));

        var child = (GenericRecord)actual[nameof(expected.Child)];
        Assert.Equal(expected.Child.Name, child[nameof(expected.Child.Name)]);
        Assert.Equal(expected.Child.Amount, child[nameof(expected.Child.Amount)]);

        Assert.Equal(expected.Color.ToString(), ((GenericEnum)actual[nameof(expected.Color)]).Value);
        Assert.Equal(expected.NullableInt, (int?)actual[nameof(expected.NullableInt)]);
        Assert.Equal(expected.DecimalValue, (decimal)(AvroDecimal)actual[nameof(expected.DecimalValue)]);

        var timestamp = (DateTime)actual[nameof(expected.Timestamp)];
        Assert.Equal(DateTimeKind.Utc, timestamp.Kind);
        Assert.Equal(expected.Timestamp.Ticks, timestamp.Ticks);

        Assert.Equal(expected.Id, (Guid)actual[nameof(expected.Id)]);
        Assert.Equal(expected.Date, DateOnly.FromDateTime((DateTime)actual[nameof(expected.Date)]));
        Assert.Equal(expected.Time.ToTimeSpan(), (TimeSpan)actual[nameof(expected.Time)]);
    }

    internal static void Equivalent(InteropRecord expected, InteropRecord actual)
    {
        Assert.Equal(expected.IntValue, actual.IntValue);
        Assert.Equal(expected.LongValue, actual.LongValue);
        Assert.Equal(expected.FloatValue, actual.FloatValue);
        Assert.Equal(expected.DoubleValue, actual.DoubleValue);
        Assert.Equal(expected.BoolValue, actual.BoolValue);
        Assert.Equal(expected.StringValue, actual.StringValue);
        Assert.Equal(expected.BytesValue, actual.BytesValue);
        Assert.Equal(expected.IntList, actual.IntList);
        Assert.Equal(expected.StringMap, actual.StringMap);
        Assert.Equal(expected.Child.Name, actual.Child.Name);
        Assert.Equal(expected.Child.Amount, actual.Child.Amount);
        Assert.Equal(expected.Color, actual.Color);
        Assert.Equal(expected.NullableInt, actual.NullableInt);
        Assert.Equal(expected.DecimalValue, actual.DecimalValue);
        Assert.Equal(expected.Timestamp.Ticks, actual.Timestamp.Ticks);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Date, actual.Date);
        Assert.Equal(expected.Time, actual.Time);
    }
}
