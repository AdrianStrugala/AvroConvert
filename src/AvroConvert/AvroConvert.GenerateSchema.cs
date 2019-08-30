namespace AvroConvert
{
    using System;
    using BuildSchema;

    public static partial class AvroConvert
    {
        public static string GenerateSchema(Type type, bool usePropertyNameAsAlias = false, bool includeOnlyDataContractMembers = false)
        {
            var reader = new ReflectionSchemaBuilder(new AvroSerializerSettings(usePropertyNameAsAlias, includeOnlyDataContractMembers)).BuildSchema(type);

            return reader.ToString();
        }
    }
}