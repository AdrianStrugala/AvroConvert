using Avro;
using Avro.Generic;
using Avro.IO;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;
using Xunit;

namespace AvroConvertInteropTests;

public class JsonEncodingInteropTests
{
    private static InteropRecord SampleWithUnionValue()
    {
        var sample = InteropRecord.Sample();
        sample.NullableInt = 7;
        return sample;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AvroConvertJsonEncoding_ApacheJsonDecoder(bool unionHasValue)
    {
        var expected = unionHasValue ? SampleWithUnionValue() : InteropRecord.Sample();
        var schemaJson = AvroConvert.GenerateSchema(typeof(InteropRecord));
        var schema = Schema.Parse(schemaJson);

        var json = AvroConvert.Avro2JsonEncoding(AvroConvert.SerializeHeadless(expected, schemaJson), schemaJson);
        var record = new GenericDatumReader<GenericRecord>(schema, schema).Read(null!, new JsonDecoder(schema, json));

        InteropAssert.Equivalent(expected, record);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ApacheJsonEncoder_AvroConvertJsonEncoding(bool unionHasValue)
    {
        var expected = unionHasValue ? SampleWithUnionValue() : InteropRecord.Sample();
        var schemaJson = AvroConvert.GenerateSchema(typeof(InteropRecord));
        var schema = (RecordSchema)Schema.Parse(schemaJson);

        using var stream = new MemoryStream();
        var encoder = new JsonEncoder(schema, stream);
        new GenericDatumWriter<GenericRecord>(schema).Write(ApacheAvro.ToGenericRecord(expected, schema), encoder);
        encoder.Flush();
        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        var actual = AvroConvert.Deserialize<InteropRecord>(AvroConvert.JsonEncoding2Avro(json, schemaJson));

        InteropAssert.Equivalent(expected, actual);
    }

    [Fact]
    public void AvroConvertJsonEncoding_EqualsApacheJsonEncoderOutput_ForUnionsAndBytes()
    {
        const string schemaJson = """
            {"type":"record","name":"R","namespace":"ns","fields":[
              {"name":"opt","type":["null","string"]},
              {"name":"none","type":["null","long"]},
              {"name":"child","type":["null",{"type":"record","name":"C","fields":[{"name":"b","type":"bytes"}]}]},
              {"name":"f","type":{"type":"fixed","name":"F","size":2}}
            ]}
            """;
        var schema = (RecordSchema)Schema.Parse(schemaJson);
        var child = new GenericRecord((RecordSchema)((UnionSchema)schema["child"].Schema).Schemas[1]);
        child.Add("b", new byte[] { 0x00, 0x41, 0xFF });
        var record = new GenericRecord(schema);
        record.Add("opt", "x");
        record.Add("none", null);
        record.Add("child", child);
        record.Add("f", new GenericFixed((FixedSchema)schema["f"].Schema, new byte[] { 0x7F, 0x80 }));

        using var stream = new MemoryStream();
        var encoder = new JsonEncoder(schema, stream);
        new GenericDatumWriter<GenericRecord>(schema).Write(record, encoder);
        encoder.Flush();
        var apacheJson = System.Text.Encoding.UTF8.GetString(stream.ToArray());

        var avroConvertJson = AvroConvert.Avro2JsonEncoding(ApacheAvro.WriteBinary(schema, record), schemaJson);

        Assert.True(Newtonsoft.Json.Linq.JToken.DeepEquals(
            Newtonsoft.Json.Linq.JToken.Parse(apacheJson),
            Newtonsoft.Json.Linq.JToken.Parse(avroConvertJson)), $"Apache: {apacheJson}\nAvroConvert: {avroConvertJson}");
    }
}

public class CanonicalFormInteropTests
{
    private const string RichSchema = """
        {"type":"record","name":"Order","namespace":"com.acme","doc":"An order","aliases":["OldOrder"],"fields":[
          {"name":"id","type":{"type":"string","logicalType":"uuid"},"doc":"identifier"},
          {"name":"placed","type":{"type":"long","logicalType":"timestamp-millis"},"order":"descending"},
          {"name":"status","type":{"type":"enum","name":"Status","symbols":["NEW","PAID"],"default":"NEW"}},
          {"name":"lines","type":{"type":"array","items":{"type":"record","name":"Line","namespace":"com.acme.lines","fields":[
             {"name":"sku","type":"string"},
             {"name":"price","type":{"type":"bytes","logicalType":"decimal","precision":10,"scale":2}},
             {"name":"hash","type":{"type":"fixed","name":"Md5","size":16}}]}}},
          {"name":"tags","type":{"type":"map","values":{"type":"int"}},"default":{}},
          {"name":"next","type":["null","Order"],"default":null},
          {"name":"previousHash","type":["null","com.acme.lines.Md5"],"default":null}
        ]}
        """;

    public static TheoryData<string> Schemas => new()
    {
        RichSchema,
        AvroConvert.GenerateSchema(typeof(InteropRecord)),
        AvroConvert.GenerateSchema(typeof(List<InteropChild>)),
        // Not compared: logical types on fixed (e.g. TimeSpan -> duration). Apache.Avro 1.12.2 writes only the fixed's
        // name instead of its definition there; see ParsingCanonicalForm tests in AvroConvertUnitTests.
        """{"type":"int"}""",
        """["null","string",{"type":"fixed","name":"x.y.Z","size":3}]"""
    };

    [Theory]
    [MemberData(nameof(Schemas))]
    public void ParsingCanonicalForm_MatchesApache(string schemaJson)
    {
        var expected = SchemaNormalization.ToParsingForm(Schema.Parse(schemaJson));

        Assert.Equal(expected, AvroSchema.Parse(schemaJson).CanonicalForm);
    }

    [Theory]
    [MemberData(nameof(Schemas))]
    public void Fingerprints_MatchApache(string schemaJson)
    {
        var schema = Schema.Parse(schemaJson);

        var avroSchema = AvroSchema.Parse(schemaJson);
        Assert.Equal(SchemaNormalization.ParsingFingerprint64(schema), avroSchema.Fingerprint);
        Assert.Equal(SchemaNormalization.ParsingFingerprint("CRC-64-AVRO", schema), avroSchema.GetFingerprint(AvroFingerprintAlgorithm.Crc64Avro));
        Assert.Equal(SchemaNormalization.ParsingFingerprint("MD5", schema), avroSchema.GetFingerprint(AvroFingerprintAlgorithm.Md5));
        Assert.Equal(SchemaNormalization.ParsingFingerprint("SHA-256", schema), avroSchema.GetFingerprint(AvroFingerprintAlgorithm.Sha256));
    }
}

public class LogicalTypeInteropTests
{
    public class Timestamps
    {
        [AvroType(AvroTypeRepresentation.TimestampNanoseconds)]
        public DateTime Nanos { get; set; }

        [AvroType(AvroTypeRepresentation.LocalTimestampMilliseconds)]
        public DateTime LocalMillis { get; set; }

        [AvroType(AvroTypeRepresentation.LocalTimestampMicroseconds)]
        public DateTime LocalMicros { get; set; }

        [AvroType(AvroTypeRepresentation.LocalTimestampNanoseconds)]
        public DateTime LocalNanos { get; set; }
    }

    public class UuidAsFixed
    {
        [AvroType(AvroTypeRepresentation.UuidFixed)]
        public Guid Id { get; set; }
    }

    [Fact]
    public void Timestamps_SchemaIsAcceptedByApache_AndValuesAreUnitsSinceEpoch()
    {
        var wallClock = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Unspecified).AddTicks(1_234_567);
        var value = new Timestamps { Nanos = wallClock, LocalMillis = wallClock, LocalMicros = wallClock, LocalNanos = wallClock };
        var schemaJson = AvroConvert.GenerateSchema(typeof(Timestamps));

        // Apache.Avro 1.12.2 knows local-timestamp-millis/micros; *-nanos are unknown to it and read as long.
        var schema = (RecordSchema)Schema.Parse(schemaJson);
        Assert.Equal("local-timestamp-millis", ((LogicalSchema)schema["LocalMillis"].Schema).LogicalTypeName);
        Assert.Equal("local-timestamp-micros", ((LogicalSchema)schema["LocalMicros"].Schema).LogicalTypeName);
        Assert.Contains(@"{""type"":""long"",""logicalType"":""timestamp-nanos""}", schemaJson);
        Assert.Contains(@"{""type"":""long"",""logicalType"":""local-timestamp-nanos""}", schemaJson);

        // Read the raw longs: the spec defines them as units since 1970-01-01T00:00:00.
        var rawSchema = (RecordSchema)Schema.Parse("""
            {"type":"record","name":"Timestamps","fields":[
              {"name":"Nanos","type":"long"},{"name":"LocalMillis","type":"long"},
              {"name":"LocalMicros","type":"long"},{"name":"LocalNanos","type":"long"}]}
            """);
        var record = ApacheAvro.ReadBinary(rawSchema, AvroConvert.SerializeHeadless(value, schemaJson));

        var ticks = wallClock.Ticks - DateTime.UnixEpoch.Ticks;
        Assert.Equal(ticks * 100, record["Nanos"]);
        Assert.Equal(ticks / TimeSpan.TicksPerMillisecond, record["LocalMillis"]);
        Assert.Equal(ticks / 10, record["LocalMicros"]);
        Assert.Equal(ticks * 100, record["LocalNanos"]);
    }

    [System.Runtime.Serialization.DataContract(Name = "Outer", Namespace = "a")]
    public class Outer
    {
        [System.Runtime.Serialization.DataMember] public TimeSpan First { get; set; }
        [System.Runtime.Serialization.DataMember] public Inner Inner { get; set; } = new();
        [System.Runtime.Serialization.DataMember] public TimeSpan Second { get; set; }
    }

    [System.Runtime.Serialization.DataContract(Name = "Inner", Namespace = "b")]
    public class Inner
    {
        [System.Runtime.Serialization.DataMember] public TimeSpan Duration { get; set; }
    }

    [Fact]
    public void RepeatedDurationAcrossNamespaces_SchemaAndDataReadByApache()
    {
        var value = new Outer { First = TimeSpan.FromSeconds(1), Second = TimeSpan.FromSeconds(2), Inner = new Inner { Duration = TimeSpan.FromSeconds(3) } };

        var (schema, records) = ApacheAvro.ReadContainer(AvroConvert.Serialize(value));

        var record = Assert.Single(records);
        // Apache resolves a by-name reference to the fixed itself (without the logical wrapper).
        static string FullName(Schema s) => s is LogicalSchema logical ? ((NamedSchema)logical.BaseSchema).Fullname : ((NamedSchema)s).Fullname;
        var outer = (RecordSchema)schema;
        Assert.Equal("a.duration", FullName(outer["First"].Schema));
        Assert.Equal("a.duration", FullName(outer["Second"].Schema));
        Assert.Equal("b.duration", FullName(((RecordSchema)outer["Inner"].Schema)["Duration"].Schema));
        Assert.NotNull(record["Inner"]);
    }

    [Fact]
    public void UuidOnFixed_IsWrittenInRfc4122ByteOrder()
    {
        var value = new UuidAsFixed { Id = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff") };
        var schemaJson = AvroConvert.GenerateSchema(typeof(UuidAsFixed));
        var rawSchema = (RecordSchema)Schema.Parse("""{"type":"record","name":"UuidAsFixed","fields":[{"name":"Id","type":{"type":"fixed","name":"uuid","size":16}}]}""");

        var record = ApacheAvro.ReadBinary(rawSchema, AvroConvert.SerializeHeadless(value, schemaJson));

        Assert.Equal(Convert.FromHexString("00112233445566778899aabbccddeeff"), ((GenericFixed)record["Id"]).Value);
        Assert.Equal(value.Id, AvroConvert.DeserializeHeadless<UuidAsFixed>(AvroConvert.SerializeHeadless(value, schemaJson), schemaJson).Id);
    }
}
