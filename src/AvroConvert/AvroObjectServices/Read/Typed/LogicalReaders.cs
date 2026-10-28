using System;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.Read.Typed
{
    /// <summary>
    /// Typed decoders for logical types, called directly from compiled read plans so that values are never boxed
    /// (the counterpart of <c>LogicalWriters</c>).
    /// </summary>
    internal static class LogicalReaders
    {
        private static readonly long EpochTicks = DateTimeExtensions.UnixEpochDateTime.Ticks;
        private static readonly int EpochDayNumber = DateTimeExtensions.UnixEpochDate.DayNumber;

        /// <summary>Ticks (100 ns) since the epoch for a timestamp counted in units of <paramref name="nanosecondsPerUnit"/>.</summary>
        internal static long TimestampTicks(long value, long nanosecondsPerUnit) =>
            nanosecondsPerUnit >= 100
                ? checked(value * (nanosecondsPerUnit / 100))
                : value / (100 / nanosecondsPerUnit);

        internal static DateTime ReadDateTime(Reader reader, long nanosecondsPerUnit, DateTimeKind kind) =>
            new(EpochTicks + TimestampTicks(reader.ReadLong(), nanosecondsPerUnit), kind);

        internal static DateTimeOffset ReadDateTimeOffset(Reader reader, long nanosecondsPerUnit) =>
            new(EpochTicks + TimestampTicks(reader.ReadLong(), nanosecondsPerUnit), TimeSpan.Zero);

        internal static DateOnly ReadDateOnlyFromTimestamp(Reader reader, long nanosecondsPerUnit) =>
            DateOnly.FromDateTime(ReadDateTime(reader, nanosecondsPerUnit, DateTimeKind.Utc));

        internal static DateOnly ReadDate(Reader reader) =>
            DateOnly.FromDayNumber(EpochDayNumber + reader.ReadInt());

        internal static DateTime ReadDateAsDateTime(Reader reader) =>
            ReadDate(reader).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        internal static TimeOnly ReadTimeMillis(Reader reader) =>
            DateTimeExtensions.UnixEpochTime.Add(TimeSpan.FromMilliseconds(reader.ReadInt()));

        internal static TimeSpan ReadTimeMillisAsTimeSpan(Reader reader) =>
            TimeSpan.FromMilliseconds(reader.ReadInt());

        internal static TimeSpan ReadTimeMicrosAsTimeSpan(Reader reader) =>
            TimeSpan.FromTicks(reader.ReadLong() * 10);

        internal static TimeOnly ReadTimeMicros(Reader reader) =>
            TimeOnly.FromTimeSpan(ReadTimeMicrosAsTimeSpan(reader));

        internal static Guid ReadUuidString(Reader reader) => Guid.Parse(reader.ReadString());

        internal static Guid ReadUuidFixed(Reader reader)
        {
            if (reader is Reader concrete)
            {
                Span<byte> bytes = stackalloc byte[16];
                concrete.ReadFixed(bytes);
                return new Guid(bytes, bigEndian: true);
            }

            var buffer = new byte[16];
            reader.ReadFixed(buffer);
            return new Guid(buffer, bigEndian: true);
        }

        /// <summary>Nanoseconds per encoded unit of a timestamp logical type; 0 when it is not a timestamp.</summary>
        internal static long NanosecondsPerUnit(LogicalTypeSchema schema) => schema switch
        {
            TimestampSchema timestamp => timestamp.NanosecondsPerUnit,
            TimestampMillisecondsSchema => 1_000_000,
            TimestampMicrosecondsSchema => 1_000,
            _ => 0
        };

        internal static bool IsLocalTimestamp(LogicalTypeSchema schema) => schema is TimestampSchema { IsLocal: true };
    }
}
