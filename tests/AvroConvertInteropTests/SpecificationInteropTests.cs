using Avro;
using Avro.Generic;
using Avro.IO;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;
using Xunit;

namespace AvroConvertInteropTests;

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
