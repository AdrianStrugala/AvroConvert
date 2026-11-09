using System;
using System.Collections.Generic;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.Avro2Json
{
    /// <summary>
    /// Avro2Json(file, schema): the schema is the reader schema and the data is resolved to it from the schema in the
    /// header. It replaced the schema in the header, so the bytes were read with a schema they were not written with.
    /// </summary>
    public class Avro2JsonReaderSchemaTests
    {
        private const string Writer = @"{""type"":""record"",""name"":""Person"",""fields"":[" +
                                      @"{""name"":""name"",""type"":""string""},{""name"":""age"",""type"":""int""},{""name"":""city"",""type"":""string""}]}";

        private static byte[] File() => AvroConvert.Serialize(
            new Dictionary<string, object> { ["name"] = "Ann", ["age"] = 42, ["city"] = "Wroclaw" }, AvroSchema.Parse(Writer));

        [Test]
        public void Field_added_in_the_reader_schema_has_its_default()
        {
            const string reader = @"{""type"":""record"",""name"":""Person"",""fields"":[" +
                                  @"{""name"":""name"",""type"":""string""},{""name"":""age"",""type"":""int""},{""name"":""city"",""type"":""string""}," +
                                  @"{""name"":""country"",""type"":""string"",""default"":""PL""}]}";

            AvroConvert.Avro2Json(File(), AvroSchema.Parse(reader)).Should().Be("{\"name\":\"Ann\",\"age\":42,\"city\":\"Wroclaw\",\"country\":\"PL\"}");
        }

        [Test]
        public void Field_left_out_of_the_reader_schema_is_skipped()
        {
            const string reader = @"{""type"":""record"",""name"":""Person"",""fields"":[{""name"":""city"",""type"":""string""}]}";

            AvroConvert.Avro2Json(File(), AvroSchema.Parse(reader)).Should().Be("{\"city\":\"Wroclaw\"}");
        }

        [Test]
        public void Promoted_type_and_alias()
        {
            const string reader = @"{""type"":""record"",""name"":""Person"",""fields"":[" +
                                  @"{""name"":""fullName"",""aliases"":[""name""],""type"":""string""},{""name"":""age"",""type"":""long""}]}";

            AvroConvert.Avro2Json(File(), AvroSchema.Parse(reader)).Should().Be("{\"fullName\":\"Ann\",\"age\":42}");
        }

        [Test]
        public void Several_entries_are_an_array()
        {
            var file = AvroConvert.Serialize(new List<Dictionary<string, object>>
            {
                new() { ["name"] = "Ann", ["age"] = 42, ["city"] = "Wroclaw" },
                new() { ["name"] = "Bob", ["age"] = 7, ["city"] = "Krakow" }
            }, AvroSchema.Parse(Writer));
            const string reader = @"{""type"":""record"",""name"":""Person"",""fields"":[{""name"":""age"",""type"":""int""}]}";

            AvroConvert.Avro2Json(file, AvroSchema.Parse(reader)).Should().Be("[{\"age\":42},{\"age\":7}]");
        }

        [Test]
        public void Entries_that_are_arrays_stay_arrays()
        {
            const string writer = @"{""type"":""array"",""items"":""int""}";
            var file = RoundTrip.RawContainer(writer, [.. RoundTrip.ZigZag(1), .. RoundTrip.ZigZag(5), 0, .. RoundTrip.ZigZag(1), .. RoundTrip.ZigZag(6), 0], 2);

            AvroConvert.Avro2Json(file, AvroSchema.Parse(@"{""type"":""array"",""items"":""long""}")).Should().Be("[[5],[6]]");
            AvroConvert.Avro2Json(file).Should().Be("[[5],[6]]");
        }

        [Test]
        public void Reader_schema_that_does_not_match_the_data_is_an_exception()
        {
            const string reader = @"{""type"":""record"",""name"":""Person"",""fields"":[{""name"":""missing"",""type"":""string""}]}";

            Recorded.Exception(() => AvroConvert.Avro2Json(File(), AvroSchema.Parse(reader))).Should().NotBeNull(
                "a field without a default that the data lacks cannot be read");
        }

        [Test]
        public void The_schema_of_the_header_gives_what_Avro2Json_without_a_schema_gives()
        {
            var file = File();

            AvroConvert.Avro2Json(file, AvroSchema.Parse(Writer)).Should().Be(AvroConvert.Avro2Json(file));
        }

        [Test]
        public void File_of_only_a_header_is_empty_text_whatever_the_schema()
        {
            var file = RoundTrip.RawContainer(Writer, Array.Empty<byte>(), 0);
            const string reader = @"{""type"":""record"",""name"":""Person"",""fields"":[{""name"":""age"",""type"":""int""}]}";

            AvroConvert.Avro2Json(file, AvroSchema.Parse(reader)).Should().Be(AvroConvert.Avro2Json(file));
        }

        /// <summary>
        /// Data of many types read through resolution gives the JSON of the reading without it: the reader schema here is the
        /// writer schema with a "doc" added, which changes no data but takes the resolving path.
        /// </summary>
        private static IEnumerable<TestCaseData> Values()
        {
            yield return new TestCaseData(@"""int""", 5);
            yield return new TestCaseData(@"""string""", "x");
            yield return new TestCaseData(@"""bytes""", new byte[] { 0, 1, 255 });
            yield return new TestCaseData(@"{""type"":""array"",""items"":""long""}", new List<long> { 1, 2 });
            yield return new TestCaseData(@"{""type"":""map"",""values"":""double""}", new Dictionary<string, double> { ["a"] = 1.5 });
            yield return new TestCaseData(@"[""null"",""string"",""int""]", 7);
            yield return new TestCaseData(@"{""type"":""fixed"",""name"":""F"",""size"":2}", new byte[] { 9, 8 });
            yield return new TestCaseData(@"{""type"":""enum"",""name"":""E"",""symbols"":[""X"",""Y""]}", "Y");
        }

        [TestCaseSource(nameof(Values))]
        public void Same_JSON_through_resolution_for_values(string schema, object value)
        {
            const string box = @"{{""type"":""record"",""name"":""Box"",{0}""fields"":[{{""name"":""v"",""type"":{1}}}]}}";
            var file = AvroConvert.Serialize(new Dictionary<string, object> { ["v"] = value }, AvroSchema.Parse(string.Format(box, "", schema)));

            AvroConvert.Avro2Json(file, AvroSchema.Parse(string.Format(box, @"""doc"":""d"",", schema))).Should().Be(AvroConvert.Avro2Json(file));
        }

        public class Rich
        {
            public DateTime When { get; set; }
            public decimal Price { get; set; }
            public Guid Id { get; set; }
            public byte[] Bytes { get; set; }
            public Kind Kind { get; set; }
            public int? Maybe { get; set; }
            public string Nothing { get; set; }
            public Dictionary<string, int> Map { get; set; }
            public List<Inner> Items { get; set; }
            public TimeSpan Span { get; set; }
        }

        public class Inner
        {
            public long Value { get; set; }
        }

        public enum Kind
        {
            A,
            B
        }

        [Test]
        public void Same_JSON_through_resolution_for_logical_types_enums_unions_and_nesting()
        {
            var file = AvroConvert.Serialize(new Rich
            {
                When = new DateTime(2026, 11, 9, 10, 0, 0, DateTimeKind.Utc),
                Price = 12.34m,
                Id = Guid.Parse("4b0a9f4e-58f1-4f0e-8d0c-0a1b2c3d4e5f"),
                Bytes = new byte[] { 1, 2, 255 },
                Kind = Kind.B,
                Maybe = null,
                Nothing = "",
                Map = new() { ["k"] = 1 },
                Items = new() { new Inner { Value = 3 } },
                Span = TimeSpan.FromMinutes(90)
            });
            var schema = AvroConvert.GetSchema(file);
            var withDoc = AvroSchema.Parse(schema.Insert(schema.IndexOf("\"fields\"", StringComparison.Ordinal), "\"doc\":\"d\","));

            AvroConvert.Avro2Json(file, withDoc).Should().Be(AvroConvert.Avro2Json(file));
        }
    }
}
