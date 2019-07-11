namespace AvroConvert.Read.AutoMapperConverters
{
    using System;
    using AutoMapper;

    public class DateTimeConverter : ITypeConverter<long, DateTime>
    {
        public DateTime Convert(long source, DateTime destination, ResolutionContext context)
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = new DateTime();
            result = result.AddSeconds(source);
            result = result.AddTicks(unixEpoch.Ticks);

            return result;
        }
    }
}