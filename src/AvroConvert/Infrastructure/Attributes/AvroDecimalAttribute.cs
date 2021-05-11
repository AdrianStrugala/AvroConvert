using System;

namespace SolTechnology.Avro.Infrastructure.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class AvroDecimalAttribute : Attribute
    {
        public virtual int Scale { get; set; }

        public virtual int Precision { get; set; }
    }
}
