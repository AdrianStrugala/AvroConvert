namespace AvroConvert
{
    using Constants;
    using System.IO;
    using Decoder;

    public static partial class AvroConvert
    {
        public static string GetSchema(byte[] avroBytes)
        {
            var reader = Reader.OpenReader(new MemoryStream(avroBytes));
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return schemaString;
        }

        public static string GetSchema(string filePath)
        {
            var reader = Reader.OpenReader(filePath);
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return schemaString;
        }

        public static string GetSchema(Stream avroStream)
        {
            var reader = Reader.OpenReader(avroStream);
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return schemaString;
        }
    }
}
