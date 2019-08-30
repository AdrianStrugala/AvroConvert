namespace AvroConvert.Write.Resolvers
{
    using System;
    using Exceptions;

    public class String
    {
        public void Resolve(object value, IWriter encoder)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!(value is string))
            {
                try
                {
                    value = value.ToString();
                }
                catch (Exception)
                {
                    throw new AvroTypeMismatchException("[String] required to write against [String] schema but found " + value.GetType());
                }
            }

            encoder.WriteString((string)value);
        }
    }
}
