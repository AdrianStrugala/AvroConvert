namespace AvroConvert.Write.Resolvers
{
    using System;

    public class String
    {
        public void Resolve(object value, IWriter encoder)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (value is Guid)
            {
                value = value.ToString();
            }

            encoder.WriteString((string)value);
        }
    }
}
