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
        Date
    }
}

