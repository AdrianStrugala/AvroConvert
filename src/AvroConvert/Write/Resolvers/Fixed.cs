namespace AvroConvert.Write.Resolvers
{
    using Exceptions;
    using Generic;
    using Schema;

    public class Fixed
    {
        public Encoder.WriteItem Resolve(FixedSchema es)
        {
            return (value, encoder) =>
            {
                if (!(value is GenericFixed) || !((GenericFixed)value).Schema.Equals(es))
                {
                    throw new AvroTypeMismatchException("[GenericFixed] required to write against [Fixed] schema but found " + value.GetType());
                }

                GenericFixed ba = (GenericFixed)value;
                encoder.WriteFixed(ba.Value);
            };
        }
    }
}
