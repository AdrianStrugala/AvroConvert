#region license
/**Copyright (c) 2022 Adrian Strugala
*
* Licensed under the CC BY-NC-SA 3.0 License(the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://creativecommons.org/licenses/by-nc-sa/3.0/
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* You are free to use or modify the code for personal usage.
* For commercial usage purchase the product at
*
* https://xabe.net/product/avroconvert/
*/
#endregion

using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
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
        public static byte[] Json2Avro<T>(string json)
        {
            var deserializedJson = JsonConvert.DeserializeObject<T>(json);
            var result = AvroConvert.Serialize(deserializedJson);

            return result;
        }


        /// <summary>
        ///  Converts Json object directly to Avro format
        /// Choosing <paramref name="codecType"/> reduces output object size
        /// </summary>
        public static byte[] Json2Avro<T>(string json, CodecType codecType)
        {
            var deserializedJson = JsonConvert.DeserializeObject<T>(json);
            var result = AvroConvert.Serialize(deserializedJson, codecType);

            return result;
        }
    }
}
