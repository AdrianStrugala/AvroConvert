namespace AvroConvert
{
    using Constants;
    using Newtonsoft.Json.Linq;
    using System.IO;

    public static partial class AvroConvert
    {
        public static JObject GetSchemaAsJObject(byte[] avroBytes)
        {
            var reader = Reader.Reader.OpenReader(new MemoryStream(avroBytes));
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return JObject.Parse(schemaString);
        }

        public static JObject GetSchemaAsJObject(string filePath)
        {
            var reader = Reader.Reader.OpenReader(filePath);
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return JObject.Parse(schemaString);
        }

        public static JObject GetSchemaAsJObject(Stream avroStream)
        {
            var reader = Reader.Reader.OpenReader(avroStream);
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return JObject.Parse(schemaString);
        }

        public static string GetSchemaAsString(byte[] avroBytes)
        {
            var reader = Reader.Reader.OpenReader(new MemoryStream(avroBytes));
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return schemaString;
        }

        public static string GetSchemaAsString(string filePath)
        {
            var reader = Reader.Reader.OpenReader(filePath);
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return schemaString;
        }

        public static string GetSchemaAsString(Stream avroStream)
        {
            var reader = Reader.Reader.OpenReader(avroStream);
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return schemaString;
        }
    }
}
