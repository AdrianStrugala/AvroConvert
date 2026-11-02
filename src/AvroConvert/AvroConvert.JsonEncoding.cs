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

using System;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Features.JsonEncoding;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Converts an Avro object container file to JSON as defined by the Avro specification ("JSON Encoding"), the format
        /// read and written by avro-tools <c>tojson</c>/<c>fromjson</c> and the JSON encoders of other Avro implementations.
        /// Unlike <see cref="Avro2Json(byte[])"/> (plain JSON shaped like the .NET model), non-null union values are wrapped
        /// in an object naming the branch, e.g. <c>{"string": "x"}</c>, bytes and fixed are strings of code points 0-255,
        /// and logical types are written as their underlying type. Each container entry is one JSON value; several entries
        /// are separated by new lines.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static string Avro2JsonEncoding(byte[] avro)
        {
            return new JsonEncodingWriter().Convert(avro, null);
        }

        /// <summary>
        /// Converts Avro data to JSON as defined by the Avro specification ("JSON Encoding"). <paramref name="avroSchema"/> is the
        /// writer schema of data without a header (headless); consecutive headless values become new-line separated JSON values.
        /// For an object container file the schema from its header is used.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static string Avro2JsonEncoding(byte[] avro, AvroSchema avroSchema)
        {
            ArgumentNullException.ThrowIfNull(avroSchema);
            return new JsonEncodingWriter().Convert(avro, avroSchema.TypeSchema);
        }

        /// <summary>
        /// Converts JSON in the Avro specification's JSON Encoding (e.g. produced by <see cref="Avro2JsonEncoding(byte[])"/>
        /// or avro-tools <c>tojson</c>) to an Avro object container file. Each JSON value in <paramref name="json"/> (separated
        /// by whitespace or new lines) becomes one entry. Missing record fields take their schema default.
        /// Of <paramref name="options"/>, only <see cref="AvroConvertOptions.Codec"/> applies.
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        public static byte[] JsonEncoding2Avro(string json, AvroSchema avroSchema, AvroConvertOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(avroSchema);
            return new JsonEncodingReader().Convert(json, avroSchema.TypeSchema, options?.Codec ?? CodecType.Null);
        }
    }
}
