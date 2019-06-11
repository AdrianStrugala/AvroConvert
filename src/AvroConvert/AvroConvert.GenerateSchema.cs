namespace AvroConvert
{
    using BuildSchema;

    public static partial class AvroConvert
    {
        public static string GenerateSchema(object obj)
        {
            var reader = new ReflectionSchemaBuilder(new AvroSerializerSettings()).BuildSchema(obj.GetType());

            return reader.ToString();
        }
    }
}