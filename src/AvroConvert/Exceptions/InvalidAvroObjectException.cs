namespace AvroConvert.Exceptions
{
    using System;

    public class InvalidAvroObjectException : Exception
    {
        public InvalidAvroObjectException(string s)
            : base(s)
        {
        }

        public InvalidAvroObjectException(string s, Exception inner)
            : base(s, inner)
        {
        }
    }
}
