using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json;
using SolTechnology.Avro.Features.JsonToAvro;
using SolTechnology.Avro.Infrastructure.Extensions;

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

            var unknownObject = JsonConvert.DeserializeObject<object>(json);
            var enumerable = unknownObject.GetType().FindEnumerableType();
            if (enumerable != null)
            {
                var childItem = ((IList)unknownObject)[0];

                var xd = JsonConvert.DeserializeObject<List<ExpandoObject>>(json, new ExpandoObjectJsonConverter());
                var result = decoder.SerializeExpando(xd, CodecType.Null);
                return result;
                //dupa
            }

            var expando = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectJsonConverter());

          

            if (expando != null && expando.Any())
            {
                var result = decoder.SerializeExpando(expando, CodecType.Null);
                return result;
            }

           

            return Serialize(unknownObject, CodecType.Null);
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
