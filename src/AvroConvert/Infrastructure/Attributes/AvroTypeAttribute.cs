using System;

namespace SolTechnology.Avro.Infrastructure.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class AvroTypeAttribute : Attribute
    {
        public AvroTypeRepresentation TypeRepresentation { get; set; }


        public AvroTypeAttribute(AvroTypeRepresentation typeRepresentation)
        {
            TypeRepresentation = typeRepresentation;
        }
    }

    public enum AvroTypeRepresentation
    {
        Null,
        Boolean,
        Int,
        Long,
        Float,
        Double,
        Bytes,
        String,
        Uuid,
        TimestampMilliseconds,
        TimestampMicroseconds,
        Decimal,
        Duration,
        TimeMilliseconds,
        TimeMicroseconds,
        Date,
        /// <summary><c>timestamp-nanos</c> (long, nanoseconds since the Unix epoch, UTC).</summary>
        TimestampNanoseconds,
        /// <summary><c>local-timestamp-millis</c> (long, milliseconds since 1970-01-01T00:00:00, wall-clock time without time zone).</summary>
        LocalTimestampMilliseconds,
        /// <summary><c>local-timestamp-micros</c> (long, microseconds since 1970-01-01T00:00:00, wall-clock time without time zone).</summary>
        LocalTimestampMicroseconds,
        /// <summary><c>local-timestamp-nanos</c> (long, nanoseconds since 1970-01-01T00:00:00, wall-clock time without time zone).</summary>
        LocalTimestampNanoseconds,
        /// <summary><c>uuid</c> on <c>fixed(16)</c> (RFC 4122 byte order) instead of the default <c>string</c>.</summary>
        UuidFixed,
        /// <summary><c>big-decimal</c> (bytes; the scale is stored with every value).</summary>
        BigDecimal
    }
}

