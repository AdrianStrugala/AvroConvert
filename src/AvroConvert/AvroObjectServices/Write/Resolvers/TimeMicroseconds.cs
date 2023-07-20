using System;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;
using Encoder = SolTechnology.Avro.Features.Serialize.Encoder;

namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers
{
    internal class TimeMicroseconds
    {
        internal Encoder.WriteItem Resolve(TimeMicrosecondsSchema schema)
        {
            return (value, encoder) =>
            {
                if (value is not TimeOnly timeOnly)
                {
                    throw new AvroTypeMismatchException(
                        $"[TimeOnly] required to write against [Long] of [time-micros] schema but found [{value.GetType()}]");
                }

                if (timeOnly > TimeMicrosecondsSchema.MaxTime)
                    throw new ArgumentOutOfRangeException(nameof(TimeMilliseconds), "A 'time-micros' value can only have the range '00:00:00' to '23:59:59'.");

                encoder.WriteLong((long)(timeOnly - DateTimeExtensions.UnixEpochTime).Ticks / 10);
            };
        }
    }
}
