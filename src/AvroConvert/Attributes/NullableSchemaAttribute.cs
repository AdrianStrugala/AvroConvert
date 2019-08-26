namespace AvroConvert.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class NullableSchemaAttribute : Attribute
    {
        private string defaultValue;

        public bool HasDefaultValue { get; private set; }

        /// <summary>
        ///     Gets the default value. (used when generating Avro Schema)
        /// </summary>
        public string DefaultValue {
            get { return defaultValue; }
            set { HasDefaultValue = true; defaultValue = value; }
        }
    }
}
