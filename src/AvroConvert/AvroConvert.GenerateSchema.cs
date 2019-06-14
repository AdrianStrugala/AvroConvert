namespace AvroConvert
{
    using System;
    using BuildSchema;

    public static partial class AvroConvert
    {
        public static string GenerateSchema(Type type)
        {
            var reader = new ReflectionSchemaBuilder(new AvroSerializerSettings()).BuildSchema(type);

            return reader.ToString();
        }
    }
}