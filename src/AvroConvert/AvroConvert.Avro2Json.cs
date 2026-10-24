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
using System.IO;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Features.AvroToJson;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Converts Avro object directly to JSON format
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
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
        /// Converts Avro object compatible with given <paramref name="avroSchema"/> directly to JSON format
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static string Avro2Json(byte[] avro, string avroSchema)
        {
            return Avro2Json(avro, AvroSchema.Parse(avroSchema));
        }

        /// <summary>
        /// Converts Avro object compatible with given <paramref name="avroSchema"/> directly to JSON format
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static string Avro2Json(byte[] avro, AvroSchema avroSchema)
        {
            using (var stream = new MemoryStream(avro))
            {
                var decoder = new Decoder();
                var deserialized = decoder.Decode(stream, avroSchema.TypeSchema);
                var json = JsonConvert.SerializeObject(deserialized);

                return json;
            }
        }
    }
}
