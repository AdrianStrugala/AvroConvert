using AwesomeAssertions;
using Avro;
using Avro.Generic;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;
using NUnit.Framework;

namespace AvroConvertInteropTests;

public class ContainerFileInteropTests
{
    public static IEnumerable<TestCaseData> Codecs => new[]
    {
        new TestCaseData("null", CodecType.Null),
        new TestCaseData("deflate", CodecType.Deflate),
        new TestCaseData("snappy", CodecType.Snappy),
        new TestCaseData("zstandard", CodecType.Zstandard)
    };

    [TestCaseSource(nameof(Codecs))]
    public void AvroConvertWrites_ApacheReads(string apacheCodec, CodecType codec)
    {
        ApacheAvro.SkipIfCodecUnavailable(apacheCodec);
        var expected = InteropRecord.Sample();

        var bytes = AvroConvert.Serialize(expected, codec);
        var (schema, records) = ApacheAvro.ReadContainer(bytes);

        schema.Tag.Should().Be(Schema.Type.Record);
        var record = records.Should().ContainSingle().Subject;
        InteropAssert.Equivalent(expected, record);
    }

    [TestCaseSource(nameof(Codecs))]
    public void ApacheWrites_AvroConvertReads(string apacheCodec, CodecType codec)
    {
        _ = codec;
        ApacheAvro.SkipIfCodecUnavailable(apacheCodec);
        var expected = InteropRecord.Sample();
        var schema = (RecordSchema)Schema.Parse(AvroConvert.GenerateSchema(typeof(InteropRecord)));

        var bytes = ApacheAvro.WriteContainer(schema, ApacheAvro.CreateCodec(apacheCodec), ApacheAvro.ToGenericRecord(expected, schema));
        var actual = AvroConvert.Deserialize<InteropRecord>(bytes);

        InteropAssert.Equivalent(expected, actual);
    }

    [Test]
    public void AvroConvertWritesList_ApacheReadsAllEntries()
    {
        var expected = SampleList(50);

        var bytes = AvroConvert.Serialize(expected, CodecType.Snappy);
        var (_, records) = ApacheAvro.ReadContainer(bytes);

        records.Count.Should().Be(expected.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            InteropAssert.Equivalent(expected[i], records[i]);
        }
    }

    [Test]
    public void AvroConvertWritesList_SingleArrayMode_ApacheReadsSingleArrayEntry()
    {
        var expected = SampleList(50);
        var options = new AvroConvertOptions { Codec = CodecType.Snappy, CollectionMode = AvroCollectionMode.SingleArray };

        var bytes = AvroConvert.Serialize(expected, options);
        var (schema, items) = ApacheAvro.ReadContainer<object[]>(bytes);

        schema.Tag.Should().Be(Schema.Type.Array);
        var array = items.Should().ContainSingle().Subject;
        array.Length.Should().Be(expected.Count);
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

    [Test]
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

        actual.Count.Should().Be(expected.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            InteropAssert.Equivalent(expected[i], actual[i]);
        }
    }

    [Test]
    public void HeaderSchema_RoundTripsThroughApacheParser()
    {
        var bytes = AvroConvert.Serialize(InteropRecord.Sample());

        var fromHeader = AvroConvert.GetSchema(bytes);
        var apache = Schema.Parse(fromHeader);

        apache.Should().BeEquivalentTo(Schema.Parse(AvroConvert.GenerateSchema(typeof(InteropRecord))));
    }
}
