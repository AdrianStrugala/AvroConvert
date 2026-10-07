using Avro;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertInteropTests;

public class BinaryEncodingInteropTests
{
    [Fact]
    public void AvroConvertHeadless_ApacheBinaryDecoder()
    {
        var expected = InteropRecord.Sample();
        var schemaJson = AvroConvert.GenerateSchema(typeof(InteropRecord));
        var schema = Schema.Parse(schemaJson);

        var bytes = AvroConvert.SerializeHeadless(expected, schemaJson);
        var record = ApacheAvro.ReadBinary(schema, bytes);

        InteropAssert.Equivalent(expected, record);
    }

    [Fact]
    public void ApacheBinaryEncoder_AvroConvertHeadless()
    {
        var expected = InteropRecord.Sample();
        var schemaJson = AvroConvert.GenerateSchema(typeof(InteropRecord));
        var schema = (RecordSchema)Schema.Parse(schemaJson);

        var bytes = ApacheAvro.WriteBinary(schema, ApacheAvro.ToGenericRecord(expected, schema));
        var actual = AvroConvert.DeserializeHeadless<InteropRecord>(bytes, schemaJson);

        InteropAssert.Equivalent(expected, actual);
    }
}

public class SchemaInteropTests
{
    [Theory]
    [InlineData(typeof(InteropRecord))]
    [InlineData(typeof(InteropChild))]
    [InlineData(typeof(List<InteropRecord>))]
    [InlineData(typeof(Dictionary<string, InteropChild>))]
    [InlineData(typeof(InteropColor))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(TimeSpan))]
    [InlineData(typeof(int?))]
    public void GeneratedSchema_IsAcceptedByApacheParser(Type type)
    {
        var json = AvroConvert.GenerateSchema(type);

        var schema = Schema.Parse(json);

        Assert.NotNull(schema);
    }

    [Fact]
    public void GeneratedSchema_TimeOnly_IsTimeMicros()
    {
        var schema = Schema.Parse(AvroConvert.GenerateSchema(typeof(TimeOnly)));

        var logical = Assert.IsType<LogicalSchema>(schema);
        Assert.Equal("time-micros", logical.LogicalTypeName);
        Assert.Equal(Schema.Type.Long, logical.BaseSchema.Tag);
    }

    [Theory]
    [InlineData(@"{""type"":""string"",""logicalType"":""datetime""}")]
    [InlineData(@"{""type"":""record"",""name"":""R"",""fields"":[{""name"":""f"",""type"":{""type"":""long"",""logicalType"":""timestamp-nanos""}}]}")]
    public void UnknownLogicalType_IsAcceptedByBothImplementations(string json)
    {
        Assert.NotNull(Schema.Parse(json));

        var model = AvroConvert.GenerateModel(json);

        Assert.NotNull(model);
    }
}
