namespace AvroConvert.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class NullableSchemaAttribute : Attribute
    {
    }
}
