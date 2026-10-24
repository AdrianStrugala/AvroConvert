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
using SolTechnology.Avro.AvroObjectServices.Schemas;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    /// <summary>
    /// Primitive type resolution between a writer and a reader schema ("Schema Resolution" in the Avro specification).
    /// </summary>
    internal static class SchemaPromotion
    {
        /// <summary>
        /// Promotions defined by the specification: int → long, float, double; long → float, double; float → double;
        /// string ↔ bytes.
        /// </summary>
        internal static bool IsSpecPromotion(AvroType writer, AvroType reader) => (writer, reader) switch
        {
            _ when writer == reader => true,
            (AvroType.Int, AvroType.Long or AvroType.Float or AvroType.Double) => true,
            (AvroType.Long, AvroType.Float or AvroType.Double) => true,
            (AvroType.Float, AvroType.Double) => true,
            (AvroType.String, AvroType.Bytes) => true,
            (AvroType.Bytes, AvroType.String) => true,
            _ => false
        };

        /// <summary>
        /// AvroConvert extensions beyond the specification, kept for compatibility with 3.x: long → int (narrowing)
        /// and null → string.
        /// </summary>
        internal static bool IsLenientConversion(AvroType writer, AvroType reader) =>
            (writer, reader) is (AvroType.Long, AvroType.Int) or (AvroType.Null, AvroType.String);

        internal static bool CanRead(AvroType writer, AvroType reader) =>
            IsSpecPromotion(writer, reader) || IsLenientConversion(writer, reader);

        /// <summary>CLR type a promoted value takes when the target carries no type of its own (<c>object</c>, dynamic).</summary>
        internal static Type ClrType(AvroType type) => type switch
        {
            AvroType.Int => typeof(int),
            AvroType.Long => typeof(long),
            AvroType.Float => typeof(float),
            AvroType.Double => typeof(double),
            AvroType.String => typeof(string),
            AvroType.Bytes => typeof(byte[]),
            _ => null
        };
    }
}
