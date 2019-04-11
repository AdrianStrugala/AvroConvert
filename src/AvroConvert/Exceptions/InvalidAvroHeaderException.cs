namespace Avro.Exceptions
{
    using System;

    public class InvalidAvroHeaderException : Exception
    {
        public InvalidAvroHeaderException()
        {
        }

        public InvalidAvroHeaderException(string s)
            : base(s)
        {
        }

        public InvalidAvroHeaderException(string s, Exception inner)
            : base(s, inner)
        {
        }
    }
}
