using System;
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
            return decoder.DecodeJson(json, CodecType.Null);

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
