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
using System.ComponentModel;
using Newtonsoft.Json;
using SolTechnology.Avro.Features.JsonToAvro;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Converts JSON object directly to Avro format
        /// Warning! The dynamic implementations is an experimental feature. Use generic overload if possible.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] Json2Avro(string json)
        {
            var decoder = new JsonToAvroDecoder();
            return decoder.DecodeJson(json, null);

        }

        /// <summary>
        ///  Converts JSON object directly to Avro format, with the given options (codec and other settings).
        /// The schema is inferred from the JSON, so the options that shape a schema (e.g. <see cref="AvroConvertOptions.CollectionMode"/>,
        /// naming policy) do not apply; an array is written as one entry per element.
        /// Warning! The dynamic implementations is an experimental feature. Use generic overload if possible.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] Json2Avro(string json, AvroConvertOptions options)
        {
            var decoder = new JsonToAvroDecoder();
            return decoder.DecodeJson(json, options);
        }


        /// <summary>
        ///  Converts JSON object directly to Avro format
        /// Choosing <paramref name="codecType"/> reduces output object size
        /// Warning! The dynamic implementations is an experimental feature. Use generic overload if possible.
        /// </summary>
        /// <remarks>Kept for 3.x code and hidden from IntelliSense. Use the overload taking <see cref="AvroConvertOptions"/>.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] Json2Avro(string json, CodecType codecType)
        {
            return Json2Avro(json, new AvroConvertOptions { Codec = codecType });
        }


        /// <summary>
        ///  Converts JSON object directly to Avro format
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] Json2Avro<T>(string json)
        {
            var deserializedJson = JsonConvert.DeserializeObject<T>(json);
            var result = AvroConvert.Serialize(deserializedJson);

            return result;
        }

        /// <summary>
        ///  Converts JSON object directly to Avro format, with the given options (codec and other settings)
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] Json2Avro<T>(string json, AvroConvertOptions options)
        {
            var deserializedJson = JsonConvert.DeserializeObject<T>(json);
            var result = AvroConvert.Serialize(deserializedJson, options);

            return result;
        }


        /// <summary>
        ///  Converts JSON object directly to Avro format
        ///  Choosing <paramref name="codecType"/> reduces output object size
        /// </summary>
        /// <remarks>Kept for 3.x code and hidden from IntelliSense. Use the overload taking <see cref="AvroConvertOptions"/>.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] Json2Avro<T>(string json, CodecType codecType)
        {
            return Json2Avro<T>(json, new AvroConvertOptions { Codec = codecType });
        }
    }
}
