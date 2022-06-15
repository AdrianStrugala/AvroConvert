using System.Dynamic;
using Newtonsoft.Json;
using SolTechnology.Avro.Features.JsonToAvro;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Converts Json object directly to Avro format
        /// </summary>
        public static byte[] Json2Avro(string json)
        {
            var decoder = new JsonToAvroDecoder();

            var expando = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectJsonConverter());
            var result = decoder.SerializeExpando(expando, CodecType.Null);

            return result;
        }


        // /// <summary>
        // /// Converts AVRO object compatible with given <paramref name="avroSchema"/> directly to JSON format
        // /// </summary>
        // public static string Avro2Json(byte[] avro, string avroSchema)
        // {
        //     using (var stream = new MemoryStream(avro))
        //     {
        //         var decoder = new Decoder();
        //         var deserialized = decoder.Decode(stream, Schema.Create(avroSchema));
        //         var json = JsonConvert.SerializeObject(deserialized);
        //
        //         return json;
        //     }
        // }
    }
}
