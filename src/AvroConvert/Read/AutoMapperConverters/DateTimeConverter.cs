using System;
using AutoMapper;

namespace SolTechnology.Avro.Read.AutoMapperConverters
{
    public class DateTimeConverter : ITypeConverter<long, DateTime>
    {
        public DateTime Convert(long source, DateTime destination, ResolutionContext context)
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = new DateTime();
            result = result.AddTicks(unixEpoch.Ticks);
            result = result.AddSeconds(source);

            return result;
        }
    }
}