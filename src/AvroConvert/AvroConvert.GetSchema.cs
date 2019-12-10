using System.IO;
using SolTechnology.Avro.Constants;
using SolTechnology.Avro.Read;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        public static string GetSchema(byte[] avroBytes)
        {
            var reader = Decoder.OpenReader(new MemoryStream(avroBytes));
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return schemaString;
        }

        public static string GetSchema(string filePath)
        {
            var reader = Decoder.OpenReader(filePath);
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return schemaString;
        }

        public static string GetSchema(Stream avroStream)
        {
            var reader = Decoder.OpenReader(avroStream);
            var schemaString = reader.GetMetaString(DataFileConstants.SchemaMetadataKey);

            return schemaString;
        }
    }
}
