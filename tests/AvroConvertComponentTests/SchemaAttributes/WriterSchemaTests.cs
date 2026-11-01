using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.SchemaAttributes
{
    /// <summary><see cref="AvroConvert.Serialize(object, AvroSchema, AvroConvertOptions)"/>: the container is written with a given schema, not with the schema of the type.</summary>
    public class WriterSchemaTests
    {
        private const string Schema = @"{""type"":""record"",""name"":""row_v2"",""namespace"":""given"",""fields"":[" +
                                      @"{""name"":""id"",""type"":""long""},{""name"":""note"",""type"":[""null"",""string""],""default"":null}]}";

        public class Row
        {
            public long id { get; set; }
            public string note { get; set; }
        }

        private static readonly Row[] Rows = { new() { id = 1, note = "a" }, new() { id = 2, note = null } };

        private static readonly AvroSchema Given = AvroSchema.Parse(Schema);

        [Test]
        public void Container_carries_the_given_schema([ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer)
        {
            var avro = RoundTrip.Write(writer, Rows, Given);

            AvroSchema.FromAvro(avro).Should().Be(AvroSchema.Parse(Schema));
            AvroSchema.FromAvro(avro).Json.Should().Contain(@"""row_v2""");
        }

        [Test]
        public void Entries_are_read_back(
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var avro = RoundTrip.Write(writer, Rows, Given);

            RoundTrip.ReadAll<Row>(reader, avro).Should().BeEquivalentTo(Rows);
        }

        [Test]
        public void Every_codec_writes_the_given_schema([ValueSource(typeof(Formats), nameof(Formats.Codecs))] CodecType codec)
        {
            var avro = AvroConvert.Serialize(Rows.ToList(), Given, new AvroConvertOptions { Codec = codec });

            AvroSchema.FromAvro(avro).Should().Be(AvroSchema.Parse(Schema));
            AvroConvert.Deserialize<List<Row>>(avro).Should().BeEquivalentTo(Rows);
        }

        [Test]
        public void A_single_object_is_one_entry()
        {
            var avro = AvroConvert.Serialize(Rows[0], Given);

            AvroConvert.Deserialize<Row>(avro).Should().BeEquivalentTo(Rows[0]);
        }

        [Test]
        public void An_array_schema_holds_the_whole_collection_in_one_entry()
        {
            var schema = $@"{{""type"":""array"",""items"":{Schema}}}";

            var avro = AvroConvert.Serialize(Rows.ToList(), AvroSchema.Parse(schema));

            AvroSchema.FromAvro(avro).Should().Be(AvroSchema.Parse(schema));
            AvroConvert.Deserialize<List<Row>>(avro).Should().BeEquivalentTo(Rows);
        }

        [Test]
        public void An_array_schema_holds_a_collection_of_objects_in_one_entry()
        {
            var schema = $@"{{""type"":""array"",""items"":{Schema}}}";
            var items = Rows.Cast<object>().ToList();

            var avro = AvroConvert.Serialize(items, AvroSchema.Parse(schema));

            AvroConvert.Deserialize<List<Row>>(avro).Should().BeEquivalentTo(Rows);
        }

        [Test]
        public void Dictionaries_are_written_as_records(
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var rows = new[] { new Dictionary<string, object> { ["id"] = 1L, ["note"] = "a" } };

            var avro = RoundTrip.Write(writer, rows, Given);

            RoundTrip.ReadAll<Row>(reader, avro).Should().BeEquivalentTo(new[] { new Row { id = 1, note = "a" } });
        }

        private const string WithDefault = @"{""type"":""record"",""name"":""with_default"",""fields"":[" +
                                           @"{""name"":""id"",""type"":""long""},{""name"":""size"",""type"":""int"",""default"":5}]}";

        public class SizedRow
        {
            public long id { get; set; }
            public int size { get; set; }
        }

        public class RowWithoutSize
        {
            public long id { get; set; }
        }

        [Test]
        public void Member_missing_for_a_field_with_a_default_gives_the_default()
        {
            var avro = AvroConvert.Serialize(new RowWithoutSize { id = 1 }, AvroSchema.Parse(WithDefault));

            AvroConvert.Deserialize<SizedRow>(avro).size.Should().Be(5);
        }

        [Test]
        public void Key_missing_for_a_field_with_a_default_gives_the_default()
        {
            var row = new Dictionary<string, object> { ["id"] = 1L };

            var avro = AvroConvert.Serialize(row, AvroSchema.Parse(WithDefault));

            AvroConvert.Deserialize<SizedRow>(avro).size.Should().Be(5);
        }

        [Test]
        public void Field_missing_in_an_ExpandoObject_gives_the_default()
        {
            dynamic row = new ExpandoObject();
            row.id = 1L;

            var avro = AvroConvert.Serialize((object)row, AvroSchema.Parse(WithDefault));

            AvroConvert.Deserialize<SizedRow>(avro).size.Should().Be(5);
        }

        [Test]
        public void Field_missing_in_a_json_object_gives_the_default()
        {
            var row = JObject.Parse(@"{""id"":1}");

            var avro = AvroConvert.Serialize(row, AvroSchema.Parse(WithDefault));

            AvroConvert.Deserialize<SizedRow>(avro).size.Should().Be(5);
        }

        [Test]
        public void Key_with_a_value_wins_over_the_default()
        {
            var row = new Dictionary<string, object> { ["id"] = 1L, ["size"] = 7 };

            var avro = AvroConvert.Serialize(row, AvroSchema.Parse(WithDefault));

            AvroConvert.Deserialize<SizedRow>(avro).size.Should().Be(7);
        }

        [Test]
        public void Null_in_a_key_is_not_a_missing_key()
        {
            var row = new Dictionary<string, object> { ["id"] = 1L, ["size"] = null };

            var act = () => AvroConvert.Serialize(row, AvroSchema.Parse(WithDefault));

            act.Should().Throw<Exception>().WithMessage("*size*");
        }

        [Test]
        public void Missing_key_of_a_field_without_a_default_is_rejected()
        {
            var row = new Dictionary<string, object> { ["size"] = 7 };

            var act = () => AvroConvert.Serialize(row, AvroSchema.Parse(WithDefault));

            act.Should().Throw<Exception>().WithMessage("*id*");
        }

        [Test]
        public void ExpandoObjects_are_written_with_the_given_schema(
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            dynamic row = new ExpandoObject();
            row.id = 1L;
            row.note = "a";

            var avro = RoundTrip.Write<ExpandoObject>(writer, new ExpandoObject[] { row }, Given);

            AvroSchema.FromAvro(avro).Should().Be(AvroSchema.Parse(Schema));
            RoundTrip.ReadAll<Row>(reader, avro).Should().BeEquivalentTo(new[] { new Row { id = 1, note = "a" } });
        }

        [Test]
        public void Items_of_different_types_are_written_with_one_schema([ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer)
        {
            var items = new object[] { new Row { id = 1 }, new Dictionary<string, object> { ["id"] = 2L, ["note"] = null } };

            var avro = RoundTrip.Write(writer, items, Given);

            AvroConvert.Deserialize<List<Row>>(avro).Select(r => r.id).Should().Equal(1, 2);
        }

        [Test]
        public void A_value_that_does_not_match_the_schema_is_rejected()
        {
            var schema = AvroSchema.Parse(@"{""type"":""record"",""name"":""R"",""fields"":[{""name"":""id"",""type"":{""type"":""array"",""items"":""int""}}]}");

            var act = () => AvroConvert.Serialize(Rows[0], schema);

            act.Should().Throw<Exception>().WithMessage("*[Array]*");
        }

        [Test]
        public void Without_it_the_schema_comes_from_the_type()
        {
            var avro = AvroConvert.Serialize(Rows[0], new AvroConvertOptions());

            AvroSchema.FromAvro(avro).Json.Should().Contain(nameof(Row));
        }

        [Test]
        public void Options_go_with_the_schema()
        {
            var options = new AvroConvertOptions { Codec = CodecType.Deflate };
            var metadata = new Dictionary<string, AvroMetadataValue> { ["key"] = "value" };

            var avro = AvroConvert.Serialize(Rows[0], Given, options, metadata);

            System.Text.Encoding.UTF8.GetString(AvroConvert.GetMetadata(avro)["avro.codec"]).Should().Be("deflate");
            AvroConvert.GetMetadata(avro).Should().ContainKey("key");
        }

        [Test]
        public void Null_schema_is_rejected_by_every_writer()
        {
            foreach (var writer in Formats.ContainerWriters)
            {
                var act = () => RoundTrip.Write(writer, Rows, (AvroSchema)null);

                act.Should().Throw<ArgumentNullException>(writer.ToString());
            }
        }

        [Test]
        public void Items_with_object_values_are_streamed_with_a_given_schema()
        {
            const string schema = @"{""type"":""map"",""values"":""string""}";
            using var output = new System.IO.MemoryStream();
            long writtenWhileEnumerating = 0;

            IEnumerable<Dictionary<string, object>> Items()
            {
                for (int i = 0; i < 20_000; i++)
                {
                    yield return new Dictionary<string, object> { ["a"] = "some text to fill a data block" };
                }

                writtenWhileEnumerating = output.Length;
            }

            AvroConvert.Serialize(Items(), output, AvroSchema.Parse(schema));

            writtenWhileEnumerating.Should().BeGreaterThan(1000);
        }
    }
}
