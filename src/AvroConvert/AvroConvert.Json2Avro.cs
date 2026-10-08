#region license
/**Copyright (c) 2019-2026 Adrian Strugała (SolTechnology)
*
* Licensed under the PolyForm Noncommercial License 1.0.0, the PolyForm Small Business License 1.0.0,
* or the SolTechnology Commercial Licence – pick the one that applies to you. See LICENSE.md:
*
* https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*
* Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)
*/
#endregion
using Newtonsoft.Json;
using SolTechnology.Avro.Features.JsonToAvro;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Converts JSON object directly to Avro format
        /// Warning! The dynamic implementations is an experimental feature. Use generic overload if possible.
        /// </summary>
        public static byte[] Json2Avro(string json)
        {
            var decoder = new JsonToAvroDecoder();
            return decoder.DecodeJson(json, CodecType.Null);

        }


        /// <summary>
        ///  Converts JSON object directly to Avro format
        /// Choosing <paramref name="codecType"/> reduces output object size
        /// Warning! The dynamic implementations is an experimental feature. Use generic overload if possible.
        /// </summary>
        public static byte[] Json2Avro(string json, CodecType codecType)
        {
            var decoder = new JsonToAvroDecoder();
            return decoder.DecodeJson(json, codecType);
        }


        /// <summary>
        ///  Converts JSON object directly to Avro format
        /// </summary>
        public static byte[] Json2Avro<T>(string json)
        {
            var deserializedJson = JsonConvert.DeserializeObject<T>(json);
            var result = AvroConvert.Serialize(deserializedJson);

            return result;
        }


        /// <summary>
        ///  Converts JSON object directly to Avro format
        ///  Choosing <paramref name="codecType"/> reduces output object size
        /// </summary>
        public static byte[] Json2Avro<T>(string json, CodecType codecType)
        {
            var deserializedJson = JsonConvert.DeserializeObject<T>(json);
            var result = AvroConvert.Serialize(deserializedJson, codecType);

            return result;
        }
    }
}
