namespace AvroConvert.Write.Resolvers
{
    using System;
    using Exceptions;

    public class Long
    {
        public void Resolve(object value, IWriter encoder)
        {
            if (value is DateTime date)
            {
                DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var truncated = new DateTime(date.Ticks - date.Ticks % TimeSpan.TicksPerSecond);
                var different = truncated - unixEpoch;

                value = (long)different.TotalSeconds;
            }

            if (!(value is long))
            {
                throw new AvroTypeMismatchException("[Long] required to write against [Long] schema but found " + value.GetType());
            }

            encoder.WriteLong((long)value);
        }
    }
}
