namespace AvroConvert
{
    using System;
    using BuildSchema;

    public static partial class AvroConvert
    {
        public static string GenerateSchema(Type type, bool usePropertyNameAsAlias = false)
        {
            var reader = new ReflectionSchemaBuilder(new AvroSerializerSettings(usePropertyNameAsAlias)).BuildSchema(type);

            return reader.ToString();
        }
    }
}