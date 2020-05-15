using System.IO;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroToJson;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        public static string Avro2Json(byte[] avro)
        {
            using (var stream = new MemoryStream(avro))
            {
                var decoder = new Decoder();
                var deserialized = decoder.Decode(stream, null);
                var json = JsonConvert.SerializeObject(deserialized);

                return json;
            }
        }

        public static string Avro2Json(byte[] avro, string avroSchema)
        {
            using (var stream = new MemoryStream(avro))
            {
                var decoder = new Decoder();
                var deserialized = decoder.Decode(stream, Schema.Schema.Parse(avroSchema));
                var json = JsonConvert.SerializeObject(deserialized);

                return json;
            }
        }
    }
}
