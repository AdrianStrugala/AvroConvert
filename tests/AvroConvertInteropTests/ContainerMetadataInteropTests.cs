using System.Text;
using AwesomeAssertions;
using Avro;
using Avro.File;
using Avro.Generic;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertInteropTests;

/// <summary>Header metadata and schema attributes, as the reference implementation sees them.</summary>
public class ContainerMetadataInteropTests
{
    private const string AnnotatedSchema = """
        {"type":"record","name":"Row","owner":"me","fields":[
            {"name":"Id","type":"long","field-id":1},
            {"name":"Tags","type":{"type":"array","items":"string","element-id":2},"field-id":3}]}
        """;

    public class Row
    {
        public long Id { get; set; }
        public List<string> Tags { get; set; }
    }

    private static readonly Row Value = new() { Id = 7, Tags = new List<string> { "a" } };

    private static readonly Dictionary<string, AvroMetadataValue> Metadata = new()
    {
        ["content"] = "data",
        ["binary"] = new byte[] { 0, 255 }
    };

    private static byte[] ApacheContainer()
    {
        var schema = (RecordSchema)Schema.Parse(AnnotatedSchema);
        var record = new GenericRecord(schema);
        record.Add("Id", 7L);
        record.Add("Tags", new object[] { "a" });

        using var stream = new MemoryStream();
        using (var writer = DataFileWriter<GenericRecord>.OpenWriter(new GenericDatumWriter<GenericRecord>(schema), stream, Codec.CreateCodec(Codec.Type.Null), leaveOpen: true))
        {
            writer.SetMeta("content", "data");
            writer.SetMeta("binary", new byte[] { 0, 255 });
            writer.Append(record);
        }

        return stream.ToArray();
    }

    [TestCase(CodecType.Null)]
    [TestCase(CodecType.Deflate)]
    [TestCase(CodecType.Snappy)]
    public void AvroConvertWritesMetadata_ApacheReads(CodecType codec)
    {
        var bytes = AvroConvert.Serialize(new[] { Value }, AvroSchema.Parse(AnnotatedSchema), new AvroConvertOptions { Codec = codec }, Metadata);

        using var reader = DataFileReader<GenericRecord>.OpenReader(new MemoryStream(bytes));

        reader.GetMetaString("content").Should().Be("data");
        reader.GetMeta("binary").Should().Equal(0, 255);
        reader.NextEntries.Should().ContainSingle().Which["Id"].Should().Be(7L);
    }

    [Test]
    public void AvroConvertWritesAttributes_ApacheSeesThem()
    {
        var bytes = AvroConvert.Serialize(new[] { Value }, AvroSchema.Parse(AnnotatedSchema), metadata: Metadata);

        var (schema, _) = ApacheAvro.ReadContainer(bytes);

        var record = (RecordSchema)schema;
        record.GetProperty("owner").Should().Be("\"me\"");
        record["Id"].GetProperty("field-id").Should().Be("1");
        record["Tags"].Schema.GetProperty("element-id").Should().Be("2");
    }

    [Test]
    public void ApacheWritesMetadata_AvroConvertReads()
    {
        var metadata = AvroConvert.GetMetadata(ApacheContainer());

        Encoding.UTF8.GetString(metadata["content"]).Should().Be("data");
        metadata["binary"].Should().Equal(0, 255);
    }

    [Test]
    public void ApacheWritesAttributes_AvroConvertKeepsThem()
    {
        var schema = AvroSchema.FromAvro(ApacheContainer()).Json;

        schema.Should().Contain("\"field-id\":1").And.Contain("\"element-id\":2").And.Contain("\"owner\":\"me\"");
    }

    [Test]
    public void ApacheWritesRecord_AvroConvertReadsItIntoADictionary()
    {
        var result = AvroConvert.Deserialize<Dictionary<string, object>>(ApacheContainer());

        result["Id"].Should().Be(7L);
        result["Tags"].Should().BeEquivalentTo(new object[] { "a" });
    }

    [Test]
    public void AvroConvertWritesDictionary_ApacheReadsARecord()
    {
        var row = new Dictionary<string, object> { ["Id"] = 7L, ["Tags"] = new List<string> { "a" } };

        var bytes = AvroConvert.Serialize(new[] { row }, AvroSchema.Parse(AnnotatedSchema), metadata: Metadata);

        var (_, records) = ApacheAvro.ReadContainer(bytes);
        records.Should().ContainSingle().Which["Id"].Should().Be(7L);
    }

    [Test]
    public void AvroConvertWritesDictionaryOfMixedValues_ApacheReadsAMap()
    {
        var value = new Dictionary<string, object> { ["a"] = "x", ["b"] = 2L, ["c"] = null };

        var bytes = AvroConvert.Serialize(value);

        var (schema, records) = ApacheAvro.ReadContainer<IDictionary<string, object>>(bytes);
        schema.Tag.Should().Be(Schema.Type.Map);
        records.Should().ContainSingle().Which.Should().BeEquivalentTo(value);
    }

    public class Small
    {
        public int X { get; set; }
    }

    public class Large
    {
        public int X { get; set; }
        public string Y { get; set; }
    }

    [Test]
    public void AvroConvertWritesRecordsSharingAField_ApacheReadsTheirOwnBranches()
    {
        var value = new Dictionary<string, object> { ["a"] = new Small { X = 1 }, ["b"] = new Large { X = 2, Y = "s" } };

        var (_, records) = ApacheAvro.ReadContainer<IDictionary<string, object>>(AvroConvert.Serialize(value));

        var map = records.Should().ContainSingle().Subject;
        ((GenericRecord)map["a"]).Schema.Name.Should().Be(nameof(Small));
        ((GenericRecord)map["b"])["Y"].Should().Be("s");
    }

    [Test]
    public void AvroConvertWritesAClassTwice_ApacheAcceptsOneDefinition()
    {
        var value = new Dictionary<string, object> { ["a"] = new Small { X = 1 }, ["b"] = new List<object> { new Small { X = 2 } } };

        var (_, records) = ApacheAvro.ReadContainer<IDictionary<string, object>>(AvroConvert.Serialize(value));

        records.Should().ContainSingle();
    }

    [Test]
    public void MergedFile_ApacheSeesWhatIsShared()
    {
        var file = AvroConvert.Serialize(new[] { Value }, AvroSchema.Parse(AnnotatedSchema), metadata: Metadata);

        var merged = AvroConvert.Merge<Row>(new[] { file, file });

        using var reader = DataFileReader<GenericRecord>.OpenReader(new MemoryStream(merged));
        reader.GetMetaString("content").Should().Be("data");
        reader.NextEntries.Should().HaveCount(2);
    }
}
