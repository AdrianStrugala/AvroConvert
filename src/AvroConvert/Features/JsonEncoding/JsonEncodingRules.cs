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

using System.Text;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace SolTechnology.Avro.Features.JsonEncoding
{
    /// <summary>Rules shared by the JSON Encoding writer and reader.</summary>
    internal static class JsonEncodingRules
    {
        /// <summary>bytes and fixed are JSON strings whose code points 0-255 are the byte values (ISO-8859-1).</summary>
        internal static readonly Encoding Bytes = Encoding.Latin1;

        /// <summary>
        /// Name of a union branch in the JSON Encoding: the full name for named types, the type name otherwise.
        /// Logical types are identified by their underlying type.
        /// </summary>
        internal static string BranchName(TypeSchema schema) => schema switch
        {
            LogicalTypeSchema logical => BranchName(logical.BaseTypeSchema),
            NullableSchema nullable => BranchName(nullable.ValueSchema),
            NamedSchema named => named.FullName,
            _ => schema.Type.ToString().ToLowerInvariant()
        };

        internal static TypeSchema Unwrap(TypeSchema schema) => schema switch
        {
            LogicalTypeSchema logical => Unwrap(logical.BaseTypeSchema),
            NullableSchema nullable => Unwrap(nullable.ValueSchema),
            _ => schema
        };
    }
}
