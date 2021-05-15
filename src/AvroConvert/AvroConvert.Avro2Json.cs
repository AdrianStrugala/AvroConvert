using System.IO;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Features.AvroToJson;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Converts AVRO object directly to JSON format
        /// </summary>
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


        /// <summary>
        /// Converts AVRO object compatible with given <paramref name="avroSchema"/> directly to JSON format
        /// </summary>
        public static string Avro2Json(byte[] avro, string avroSchema)
        {
            using (var stream = new MemoryStream(avro))
            {
                var decoder = new Decoder();
                var deserialized = decoder.Decode(stream, Schema.Create(avroSchema));
                var json = JsonConvert.SerializeObject(deserialized);

                return json;
            }
        }
    }
}
