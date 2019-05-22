namespace AvroConvert.Write.Resolvers
{
    using Exceptions;

    public class Null
    {
        public void Resolve(object value, IWriter encoder)
        {
            if (value != null)
            {
                throw new AvroTypeMismatchException("[Null] required to write against [Null] schema but found " + value.GetType());
            }
            encoder.WriteNull();
        }
    }
}
