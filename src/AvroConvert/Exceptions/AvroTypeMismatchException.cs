using System;

namespace SolTechnology.Avro.Exceptions
{
    public class AvroTypeMismatchException : Exception
    {
        public AvroTypeMismatchException(string s)
            : base(s)
        {

        }
        public AvroTypeMismatchException(string s, Exception inner)
            : base(s, inner)
        {

        }
    }
}
