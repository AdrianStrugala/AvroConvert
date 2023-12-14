using System;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;
using Encoder = SolTechnology.Avro.Features.Serialize.Encoder;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal partial class WriteResolver
    {
        internal Encoder.WriteItem ResolveTimeMicroseconds(TimeMicrosecondsSchema schema)
        {
            return (value, encoder) =>
            {
                if (value is not TimeOnly timeOnly)
                {
                    throw new AvroTypeMismatchException(
                        $"[TimeOnly] required to write against [Long] of [time-micros] schema but found [{value.GetType()}]");
                }

                if (timeOnly > TimeMicrosecondsSchema.MaxTime)
                    throw new ArgumentOutOfRangeException("value", "A 'time-micros' value can only have the range '00:00:00' to '23:59:59'.");

                encoder.WriteLong((long)(timeOnly - DateTimeExtensions.UnixEpochTime).Ticks / 10);
            };
        }
    }
}
