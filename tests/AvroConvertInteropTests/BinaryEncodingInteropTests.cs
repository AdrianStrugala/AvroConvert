using AwesomeAssertions;
using Avro;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertInteropTests;

public class BinaryEncodingInteropTests
{
    [Test]
    public void AvroConvertHeadless_ApacheBinaryDecoder()
    {
        var expected = InteropRecord.Sample();
        var schemaJson = AvroConvert.GenerateSchema(typeof(InteropRecord));
        var schema = Schema.Parse(schemaJson);

        var bytes = AvroConvert.SerializeHeadless(expected, schemaJson);
        var record = ApacheAvro.ReadBinary(schema, bytes);

        InteropAssert.Equivalent(expected, record);
    }

    [Test]
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
    [TestCase(typeof(InteropRecord))]
    [TestCase(typeof(InteropChild))]
    [TestCase(typeof(List<InteropRecord>))]
    [TestCase(typeof(Dictionary<string, InteropChild>))]
    [TestCase(typeof(InteropColor))]
    [TestCase(typeof(decimal))]
    [TestCase(typeof(Guid))]
    [TestCase(typeof(DateTime))]
    [TestCase(typeof(DateOnly))]
    [TestCase(typeof(TimeOnly))]
    [TestCase(typeof(TimeSpan))]
    [TestCase(typeof(int?))]
    public void GeneratedSchema_IsAcceptedByApacheParser(Type type)
    {
        var json = AvroConvert.GenerateSchema(type);

        var schema = Schema.Parse(json);

        schema.Should().NotBeNull();
    }

    [Test]
    public void GeneratedSchema_TimeOnly_IsTimeMicros()
    {
        var schema = Schema.Parse(AvroConvert.GenerateSchema(typeof(TimeOnly)));

        var logical = schema.Should().BeOfType<LogicalSchema>().Subject;
        logical.LogicalTypeName.Should().Be("time-micros");
        logical.BaseSchema.Tag.Should().Be(Schema.Type.Long);
    }

    [TestCase(@"{""type"":""string"",""logicalType"":""datetime""}")]
    [TestCase(@"{""type"":""record"",""name"":""R"",""fields"":[{""name"":""f"",""type"":{""type"":""long"",""logicalType"":""timestamp-nanos""}}]}")]
    public void UnknownLogicalType_IsAcceptedByBothImplementations(string json)
    {
        Schema.Parse(json).Should().NotBeNull();

        var model = AvroConvert.GenerateModel(json);

        model.Should().NotBeNull();
    }
}
