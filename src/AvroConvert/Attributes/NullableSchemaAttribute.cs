using System;

namespace SolTechnology.Avro.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class NullableSchemaAttribute : Attribute
    {
    }
}
