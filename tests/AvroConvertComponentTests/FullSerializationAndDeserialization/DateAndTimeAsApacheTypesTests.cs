using System;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    /// <summary>DateTime for "date" and TimeSpan for "time-*", the types that Apache.Avro (avrogen) generates.</summary>
    public class DateAndTimeAsApacheTypesTests
    {
        private const string Schema = """
            {"type":"record","name":"Slot","fields":[
              {"name":"Day","type":{"type":"int","logicalType":"date"}},
              {"name":"OptionalDay","type":["null",{"type":"int","logicalType":"date"}]},
              {"name":"StartMillis","type":{"type":"int","logicalType":"time-millis"}},
              {"name":"StartMicros","type":{"type":"long","logicalType":"time-micros"}}]}
            """;

        public class Slot
        {
            public DateTime Day { get; set; }
            public DateTime? OptionalDay { get; set; }
            public TimeSpan StartMillis { get; set; }
            public TimeSpan StartMicros { get; set; }
        }

        public class SlotNew
        {
            public DateOnly Day { get; set; }
            public DateOnly? OptionalDay { get; set; }
            public TimeOnly StartMillis { get; set; }
            public TimeOnly StartMicros { get; set; }
        }

        [Test]
        public void DateTime_and_TimeSpan_round_trip()
        {
            var slot = new Slot
            {
                Day = new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc),
                OptionalDay = new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc),
                StartMillis = new TimeSpan(0, 13, 14, 15, 123),
                StartMicros = new TimeSpan(0, 13, 14, 15, 123).Add(TimeSpan.FromTicks(4560)),
            };

            var data = AvroConvert.SerializeHeadless(slot, Schema);
            var result = AvroConvert.DeserializeHeadless<Slot>(data, Schema);

            result.Should().BeEquivalentTo(slot);
            result.Day.Kind.Should().Be(DateTimeKind.Utc);
        }

        [Test]
        public void The_same_bytes_as_DateOnly_and_TimeOnly()
        {
            var slot = new Slot
            {
                Day = new DateTime(2026, 10, 28),
                OptionalDay = null,
                StartMillis = new TimeSpan(13, 14, 15),
                StartMicros = new TimeSpan(13, 14, 15),
            };
            var slotNew = new SlotNew
            {
                Day = new DateOnly(2026, 10, 28),
                OptionalDay = null,
                StartMillis = new TimeOnly(13, 14, 15),
                StartMicros = new TimeOnly(13, 14, 15),
            };

            AvroConvert.SerializeHeadless(slot, Schema).Should().Equal(AvroConvert.SerializeHeadless(slotNew, Schema));
        }
    }
}
