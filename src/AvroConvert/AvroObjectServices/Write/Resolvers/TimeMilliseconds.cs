using System;
using System.Collections.Generic;
using System.Text;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;
using Encoder = SolTechnology.Avro.Features.Serialize.Encoder;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal partial class WriteResolver
    {
        internal Encoder.WriteItem ResolveTimeMilliseconds(TimeMillisecondsSchema schema)
        {
            return (value, encoder) =>
            {
                if (value is not TimeOnly timeOnly)
                {
                    throw new AvroTypeMismatchException(
                        $"[TimeOnly] required to write against [Int] of [time-millis] schema but found [{value.GetType()}]");
                }
                
                if (timeOnly > TimeMillisecondsSchema.MaxTime)
                    throw new ArgumentOutOfRangeException(nameof(value), "A 'time-millis' value can only have the range '00:00:00' to '23:59:59'.");

                encoder.WriteInt((int)(timeOnly - DateTimeExtensions.UnixEpochTime).TotalMilliseconds);
            };
        }
    }
}
