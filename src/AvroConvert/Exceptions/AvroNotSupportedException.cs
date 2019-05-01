namespace EhwarSoft.Avro.Exceptions
{
    using System;

    public class AvroNotSupportedException : Exception
    {
        public AvroNotSupportedException(string s)
            : base(s)
        {

        }
        public AvroNotSupportedException(string s, Exception inner)
            : base(s, inner)
        {

        }
    }
}
