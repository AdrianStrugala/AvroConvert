using System;
using System.Globalization;
using System.Threading;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    /// <summary>Date and time types in an Avro "string" field: ISO 8601 text, as written by Chr.Avro by default.</summary>
    public class TemporalAsStringTests
    {
        private const string TextSchema = """
            {"type":"record","name":"Event","fields":[
              {"name":"DateTime","type":"string"},
              {"name":"NullableDateTime","type":["null","string"]},
              {"name":"DateTimeOffset","type":"string"},
              {"name":"TimeSpan","type":"string"},
              {"name":"DateOnly","type":"string"},
              {"name":"TimeOnly","type":"string"}]}
            """;

        public class Event
        {
            public DateTime DateTime { get; set; }
            public DateTime? NullableDateTime { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
            public TimeSpan TimeSpan { get; set; }
            public DateOnly DateOnly { get; set; }
            public TimeOnly TimeOnly { get; set; }
        }

        public class EventText
        {
            public string DateTime { get; set; }
            public string NullableDateTime { get; set; }
            public string DateTimeOffset { get; set; }
            public string TimeSpan { get; set; }
            public string DateOnly { get; set; }
            public string TimeOnly { get; set; }
        }

        private static Event Sample() => new()
        {
            DateTime = new DateTime(2026, 10, 28, 12, 34, 56, 789, DateTimeKind.Utc),
            NullableDateTime = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified),
            DateTimeOffset = new DateTimeOffset(2026, 10, 28, 14, 34, 56, TimeSpan.FromHours(2)),
            TimeSpan = TimeSpan.FromMinutes(90),
            DateOnly = new DateOnly(2026, 10, 28),
            TimeOnly = new TimeOnly(13, 14, 15),
        };

        [Test]
        public void Writes_iso_8601_text_independent_of_the_culture()
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("pl-PL");

                var data = AvroConvert.SerializeHeadless(Sample(), TextSchema);
                var text = AvroConvert.DeserializeHeadless<EventText>(data, TextSchema);

                text.DateTime.Should().Be("2026-10-28T12:34:56.7890000Z");
                text.NullableDateTime.Should().Be("2026-01-02T03:04:05.0000000");
                text.DateTimeOffset.Should().Be("2026-10-28T14:34:56.0000000+02:00");
                text.TimeSpan.Should().Be("PT1H30M");
                text.DateOnly.Should().Be("2026-10-28");
                text.TimeOnly.Should().Be("13:14:15.0000000");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        [Test]
        public void Reads_iso_8601_text_into_date_and_time_types()
        {
            var expected = Sample();

            var data = AvroConvert.SerializeHeadless(Sample(), TextSchema);
            var result = AvroConvert.DeserializeHeadless<Event>(data, TextSchema);

            result.Should().BeEquivalentTo(expected);
            result.DateTime.Kind.Should().Be(DateTimeKind.Utc);
            result.NullableDateTime!.Value.Kind.Should().Be(DateTimeKind.Unspecified);
            result.DateTimeOffset.Offset.Should().Be(TimeSpan.FromHours(2));
        }

        [Test]
        public void Text_written_with_an_old_schema_is_resolved_against_logical_types()
        {
            // Writer schema with strings (e.g. registered by Chr.Avro), reader schema generated from the type.
            var expected = Sample();
            var data = AvroConvert.SerializeHeadless(Sample(), TextSchema);

            var result = AvroConvert.DeserializeHeadless<Event>(data, AvroSchema.Parse(TextSchema), AvroSchema.Create<Event>());

            result.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void Null_in_a_nullable_text_field_stays_null()
        {
            var value = Sample();
            value.NullableDateTime = null;

            var data = AvroConvert.SerializeHeadless(value, TextSchema);
            var result = AvroConvert.DeserializeHeadless<Event>(data, AvroSchema.Parse(TextSchema), AvroSchema.Create<Event>());

            result.NullableDateTime.Should().BeNull();
        }

        [Test]
        public void Reads_shorter_iso_8601_forms()
        {
            var text = new EventText
            {
                DateTime = "2026-10-28T12:00:00Z",
                NullableDateTime = "2026-10-28T12:00:00",
                DateTimeOffset = "2026-10-28T12:00:00+02:00",
                TimeSpan = "01:30:00",
                DateOnly = "2026-10-28",
                TimeOnly = "13:14",
            };

            var data = AvroConvert.SerializeHeadless(text, TextSchema);
            var result = AvroConvert.DeserializeHeadless<Event>(data, TextSchema);

            result.DateTime.Should().Be(new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc));
            result.NullableDateTime.Should().Be(new DateTime(2026, 10, 28, 12, 0, 0));
            result.DateTimeOffset.Should().Be(new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.FromHours(2)));
            result.TimeSpan.Should().Be(TimeSpan.FromMinutes(90));
            result.TimeOnly.Should().Be(new TimeOnly(13, 14));
        }

        [Test]
        public void A_logical_branch_of_a_union_is_still_preferred_over_text()
        {
            const string schema = """
                {"type":"record","name":"Stamp","fields":[
                  {"name":"At","type":["null","string",{"type":"long","logicalType":"timestamp-micros"}]},
                  {"name":"AtOffset","type":["null","string",{"type":"long","logicalType":"timestamp-micros"}]}]}
                """;
            var at = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

            var data = AvroConvert.SerializeHeadless(new Stamp { At = at, AtOffset = new DateTimeOffset(at) }, schema);

            data[0].Should().Be(4, "union index 2 (the timestamp branch), zig-zag encoded");
            AvroConvert.Avro2JsonEncoding(data, schema).Should().NotContain("\"string\"", "neither DateTime nor DateTimeOffset goes to the text branch");
            var result = AvroConvert.DeserializeHeadless<Stamp>(data, schema);
            result.At.Should().Be(at);
            result.AtOffset.Should().Be(new DateTimeOffset(at));
        }

        public class Stamp
        {
            public DateTime? At { get; set; }
            public DateTimeOffset? AtOffset { get; set; }
        }

        [TestCase("en-GB", "05/10/2026 11:00:00 +01:00")]
        [TestCase("de-DE", "05.10.2026 11:00:00 +01:00")]
        public void DateTimeOffset_text_in_the_current_culture_is_read(string cultureName, string text)
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);
                var data = AvroConvert.SerializeHeadless(new EventText
                {
                    DateTime = "2026-10-05T11:00:00", DateTimeOffset = text, TimeSpan = "PT1H", DateOnly = "2026-10-05", TimeOnly = "11:00"
                }, TextSchema);

                var result = AvroConvert.DeserializeHeadless<Event>(data, TextSchema);

                result.DateTimeOffset.Should().Be(new DateTimeOffset(2026, 10, 5, 11, 0, 0, TimeSpan.FromHours(1)));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        [Test]
        public void Date_and_time_text_in_the_current_culture_is_read()
        {
            var culture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB");
                var data = AvroConvert.SerializeHeadless(new EventText
                {
                    DateTime = "01/02/2026 10:00:00", DateTimeOffset = "2026-02-01T10:00:00+00:00", TimeSpan = "PT1H", DateOnly = "01/02/2026", TimeOnly = "10:00:00"
                }, TextSchema);

                var result = AvroConvert.DeserializeHeadless<Event>(data, TextSchema);

                result.DateTime.Should().Be(new DateTime(2026, 2, 1, 10, 0, 0));
                result.DateOnly.Should().Be(new DateOnly(2026, 2, 1));
                result.TimeOnly.Should().Be(new TimeOnly(10, 0));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }
    }
}
