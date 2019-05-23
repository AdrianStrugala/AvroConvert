namespace AvroConvert.Write.Resolvers
{
    using Exceptions;
    using Generic;
    using Schema;

    public class Enum
    {
        public Encoder.WriteItem Resolve(EnumSchema es)
        {
            return (value, e) =>
            {
                if (!(value is GenericEnum) || !((GenericEnum)value).Schema.Equals(es))
                {
                    throw new AvroTypeMismatchException(
                        "[GenericEnum] required to write against [Enum] schema but found " + value.GetType());
                }

                e.WriteEnum(es.Ordinal(((GenericEnum)value).Value));
            };
        }
    }
}
