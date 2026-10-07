using Avro;
using Avro.Generic;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertInteropTests;

public class ContainerFileInteropTests
{
    public static TheoryData<string, CodecType> Codecs => new()
    {
        { "null", CodecType.Null },
        { "deflate", CodecType.Deflate },
        { "snappy", CodecType.Snappy }
    };

    [Theory]
    [MemberData(nameof(Codecs))]
    public void AvroConvertWrites_ApacheReads(string apacheCodec, CodecType codec)
    {
        _ = apacheCodec;
        var expected = InteropRecord.Sample();

        var bytes = AvroConvert.Serialize(expected, codec);
        var (schema, records) = ApacheAvro.ReadContainer(bytes);

        Assert.Equal(Schema.Type.Record, schema.Tag);
        var record = Assert.Single(records);
        InteropAssert.Equivalent(expected, record);
    }

    [Theory]
    [MemberData(nameof(Codecs))]
    public void ApacheWrites_AvroConvertReads(string apacheCodec, CodecType codec)
    {
        _ = codec;
        var expected = InteropRecord.Sample();
        var schema = (RecordSchema)Schema.Parse(AvroConvert.GenerateSchema(typeof(InteropRecord)));

        var bytes = ApacheAvro.WriteContainer(schema, ApacheAvro.CreateCodec(apacheCodec), ApacheAvro.ToGenericRecord(expected, schema));
        var actual = AvroConvert.Deserialize<InteropRecord>(bytes);

        InteropAssert.Equivalent(expected, actual);
    }

    [Fact(Skip = "#118: a top-level collection is written as a single array object; becomes N container entries in the typed-core phase")]
    public void AvroConvertWritesList_ApacheReadsAllEntries()
    {
        var expected = SampleList(50);

        var bytes = AvroConvert.Serialize(expected, CodecType.Snappy);
        var (_, records) = ApacheAvro.ReadContainer(bytes);

        Assert.Equal(expected.Count, records.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            InteropAssert.Equivalent(expected[i], records[i]);
        }
    }

    [Fact]
    public void AvroConvertWritesList_ApacheReadsSingleArrayEntry()
    {
        var expected = SampleList(50);

        var bytes = AvroConvert.Serialize(expected, CodecType.Snappy);
        var (schema, items) = ApacheAvro.ReadContainer<object[]>(bytes);

        Assert.Equal(Schema.Type.Array, schema.Tag);
        var array = Assert.Single(items);
        Assert.Equal(expected.Count, array.Length);
        for (int i = 0; i < expected.Count; i++)
        {
            InteropAssert.Equivalent(expected[i], (GenericRecord)array[i]);
        }
    }

    private static List<InteropRecord> SampleList(int count) =>
        Enumerable.Range(0, count).Select(i =>
        {
            var r = InteropRecord.Sample();
            r.IntValue = i;
            r.NullableInt = i % 2 == 0 ? i : null;
            return r;
        }).ToList();

    [Fact]
    public void ApacheWritesMultipleBlocks_AvroConvertReadsAll()
    {
        var schema = (RecordSchema)Schema.Parse(AvroConvert.GenerateSchema(typeof(InteropRecord)));
        var expected = Enumerable.Range(0, 200).Select(i =>
        {
            var r = InteropRecord.Sample();
            r.LongValue = i;
            return r;
        }).ToList();

        var bytes = ApacheAvro.WriteContainer(schema, ApacheAvro.CreateCodec("deflate"), expected.Select(r => ApacheAvro.ToGenericRecord(r, schema)).ToArray());
        var actual = AvroConvert.Deserialize<List<InteropRecord>>(bytes);

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            InteropAssert.Equivalent(expected[i], actual[i]);
        }
    }

    [Fact]
    public void HeaderSchema_RoundTripsThroughApacheParser()
    {
        var bytes = AvroConvert.Serialize(InteropRecord.Sample());

        var fromHeader = AvroConvert.GetSchema(bytes);
        var apache = Schema.Parse(fromHeader);

        Assert.Equal(Schema.Parse(AvroConvert.GenerateSchema(typeof(InteropRecord))), apache);
    }
}
