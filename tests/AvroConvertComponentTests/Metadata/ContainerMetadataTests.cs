using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace AvroConvertComponentTests.Metadata
{
    /// <summary>The key-value metadata of the container file header.</summary>
    public class ContainerMetadataTests
    {
        public class Row
        {
            public long Id { get; set; }
            public string Name { get; set; }
        }

        private static readonly Row[] Rows = { new() { Id = 1, Name = "a" }, new() { Id = 2, Name = "b" } };

        private static readonly AvroSchema RowSchema = AvroSchema.Create<Row>();

        private static Dictionary<string, AvroMetadataValue> Keys() => new()
        {
            ["format-version"] = "2",
            ["content"] = "data"
        };

        private static byte[] WithKey(string key, AvroMetadataValue value) =>
            AvroConvert.Serialize(Rows[0], RowSchema, metadata: new Dictionary<string, AvroMetadataValue> { [key] = value });

        private static IReadOnlyDictionary<string, byte[]> Read(ContainerSource source, byte[] avro) =>
            RoundTrip.FromSource(source, avro, AvroConvert.GetMetadata, AvroConvert.GetMetadata);

        private static string Text(IReadOnlyDictionary<string, byte[]> metadata, string key) => Encoding.UTF8.GetString(metadata[key]);

        [Test]
        public void Keys_written_to_the_header_are_read_back(
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerSources))] ContainerSource source)
        {
            var avro = RoundTrip.Write(writer, Rows, RowSchema, metadata: Keys());

            var metadata = Read(source, avro);

            Text(metadata, "format-version").Should().Be("2");
            Text(metadata, "content").Should().Be("data");
        }

        [Test]
        public void Every_codec_keeps_the_keys([ValueSource(typeof(Formats), nameof(Formats.Codecs))] CodecType codec)
        {
            var avro = AvroConvert.Serialize(Rows.ToList(), RowSchema, new AvroConvertOptions { Codec = codec }, Keys());

            Text(AvroConvert.GetMetadata(avro), "content").Should().Be("data");
            AvroConvert.Deserialize<List<Row>>(avro).Should().BeEquivalentTo(Rows);
        }

        [Test]
        public void Data_is_read_as_if_there_were_no_additional_keys(
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var avro = RoundTrip.Write(writer, Rows, RowSchema, metadata: Keys());

            RoundTrip.ReadAll<Row>(reader, avro).Should().BeEquivalentTo(Rows);
        }

        [Test]
        public void Json_conversions_are_not_affected([ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer)
        {
            var avro = RoundTrip.Write(writer, Rows, RowSchema, metadata: Keys());
            var plain = RoundTrip.Write(writer, Rows);

            AvroConvert.Avro2Json(avro).Should().Be(AvroConvert.Avro2Json(plain));
            AvroConvert.Avro2JsonEncoding(avro).Should().Be(AvroConvert.Avro2JsonEncoding(plain));
        }

        [Test]
        public void File_without_entries_keeps_the_keys([ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer)
        {
            var avro = RoundTrip.Write(writer, Array.Empty<Row>(), RowSchema, metadata: Keys());

            Text(AvroConvert.GetMetadata(avro), "content").Should().Be("data");
        }

        [Test]
        public void Values_can_be_any_bytes([ValueSource(typeof(Formats), nameof(Formats.ContainerSources))] ContainerSource source)
        {
            var bytes = new byte[] { 0, 255, 128, 13, 10 };

            var avro = WithKey("binary", bytes);

            Read(source, avro)["binary"].Should().Equal(bytes);
        }

        [Test]
        public void Text_goes_in_as_utf8()
        {
            var avro = WithKey("text", "żółć");

            AvroConvert.GetMetadata(avro)["text"].Should().Equal(0xC5, 0xBC, 0xC3, 0xB3, 0xC5, 0x82, 0xC4, 0x87);
        }

        [Test]
        public void Empty_value_is_a_value()
        {
            var avro = WithKey("empty", "");

            AvroConvert.GetMetadata(avro)["empty"].Should().BeEmpty();
        }

        [Test]
        public void Default_value_is_empty()
        {
            var avro = WithKey("empty", default);

            AvroConvert.GetMetadata(avro)["empty"].Should().BeEmpty();
        }

        [Test]
        public void Null_value_is_rejected_when_it_is_set()
        {
            var metadata = new Dictionary<string, AvroMetadataValue>();

            var fromText = () => metadata["key"] = (string)null;
            var fromBytes = () => metadata["key"] = (byte[])null;

            fromText.Should().Throw<ArgumentNullException>();
            fromBytes.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void The_whole_header_is_returned([ValueSource(typeof(Formats), nameof(Formats.ContainerSources))] ContainerSource source)
        {
            var avro = AvroConvert.Serialize(Rows[0], RowSchema, new AvroConvertOptions { Codec = CodecType.Deflate }, Keys());

            var metadata = Read(source, avro);

            metadata.Keys.Should().BeEquivalentTo("avro.schema", "avro.codec", "format-version", "content");
            Text(metadata, "avro.schema").Should().Be(AvroConvert.GetSchema(avro));
            Text(metadata, "avro.codec").Should().Be("deflate");
        }

        [Test]
        public void File_without_additional_keys_has_only_the_keys_of_avro()
        {
            AvroConvert.GetMetadata(AvroConvert.Serialize(Rows[0])).Keys.Should().BeEquivalentTo("avro.schema", "avro.codec");
        }

        [Test]
        public void Data_that_is_not_a_container_is_rejected([ValueSource(typeof(Formats), nameof(Formats.ContainerSources))] ContainerSource source)
        {
            var act = () => Read(source, new byte[] { 1, 2, 3 });

            act.Should().Throw<InvalidAvroObjectException>();
        }

        [TestCase("avro.schema")]
        [TestCase("avro.codec")]
        [TestCase("avro.sync")]
        [TestCase("avro.anything")]
        public void Keys_reserved_by_the_specification_are_rejected(string key)
        {
            foreach (var writer in Formats.ContainerWriters)
            {
                var metadata = new Dictionary<string, AvroMetadataValue> { [key] = "value" };

                var act = () => RoundTrip.Write(writer, Rows, RowSchema, metadata: metadata);

                act.Should().Throw<ArgumentException>().WithMessage($"*{key}*", writer.ToString());
            }
        }

        [Test]
        public void Rejected_key_leaves_the_output_untouched()
        {
            var metadata = new Dictionary<string, AvroMetadataValue> { ["avro.schema"] = "{}" };
            using var output = new MemoryStream();

            var act = () => AvroConvert.Serialize<Row>(Rows, output, RowSchema, metadata: metadata);

            act.Should().Throw<ArgumentException>();
            output.Length.Should().Be(0);
        }

        [TestCase("avro")]
        [TestCase("avroconvert.key")]
        [TestCase("AVRO.key")]
        [TestCase("my.avro.key")]
        public void Keys_outside_of_the_reserved_namespace_are_accepted(string key)
        {
            var avro = WithKey(key, "value");

            Text(AvroConvert.GetMetadata(avro), key).Should().Be("value");
        }

        [Test]
        public void Calls_sharing_schema_and_options_keep_their_own_keys()
        {
            var options = new AvroConvertOptions();

            var firstAvro = AvroConvert.Serialize(Rows[0], RowSchema, options, new Dictionary<string, AvroMetadataValue> { ["key"] = "first" });
            var secondAvro = AvroConvert.Serialize(Rows[0], RowSchema, options, new Dictionary<string, AvroMetadataValue> { ["key"] = "second" });

            Text(AvroConvert.GetMetadata(firstAvro), "key").Should().Be("first");
            Text(AvroConvert.GetMetadata(secondAvro), "key").Should().Be("second");
        }

        public class Entry
        {
            public long Id { get; set; }
            public object Value { get; set; }
        }

        [Test]
        public void Keys_go_with_the_schema_of_all_the_entries_holding_object_values([ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer)
        {
            var entries = new[] { new Entry { Id = 1, Value = "text" }, new Entry { Id = 2, Value = 7L } };

            var avro = RoundTrip.Write(writer, entries, AvroSchema.FromItems(entries), metadata: Keys());

            Text(AvroConvert.GetMetadata(avro), "content").Should().Be("data");
            AvroConvert.Deserialize<List<Entry>>(avro).Should().BeEquivalentTo(entries);
        }

        [Test]
        public void Attributes_of_the_given_schema_stay_next_to_the_keys([ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer)
        {
            const string schema = @"{""type"":""record"",""name"":""Row"",""fields"":[{""name"":""Id"",""type"":""long"",""field-id"":1},{""name"":""Name"",""type"":""string"",""field-id"":2}]}";
            var avro = RoundTrip.Write(writer, Rows, AvroSchema.Parse(schema), metadata: Keys());

            var metadata = AvroConvert.GetMetadata(avro);
            Text(metadata, "content").Should().Be("data");
            Text(metadata, "avro.schema").Should().Contain("field-id");
        }
    }
}
