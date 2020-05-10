using System;
using System.IO;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroToJson;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        public static string Avro2Json(byte[] avro, string schema = null)
        {
            var reader = Decoder.OpenReader(
                new MemoryStream(avro),
                schema);

            var deserialized = reader.Read();
            var json = JsonConvert.SerializeObject(deserialized);

            return json;
        }
    }
}
