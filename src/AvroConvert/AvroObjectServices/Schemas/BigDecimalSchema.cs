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
using System.Buffers.Binary;
using System.IO;
using System.Numerics;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;

namespace SolTechnology.Avro.AvroObjectServices.Schemas
{
    /// <summary>
    /// <c>big-decimal</c> (Avro 1.12): annotates <c>bytes</c>; the scale travels with each value. The bytes hold the
    /// Avro binary encoding of the unscaled value (<c>bytes</c>, two's complement big-endian) followed by the scale (<c>int</c>),
    /// the layout used by the Java and Rust implementations.
    /// </summary>
    internal sealed class BigDecimalSchema : LogicalTypeSchema
    {
        internal BigDecimalSchema() : this(typeof(decimal))
        {
        }

        internal BigDecimalSchema(Type runtimeType) : base(runtimeType)
        {
            BaseTypeSchema = new BytesSchema();
        }

        internal override AvroType Type => AvroType.Logical;
        internal override TypeSchema BaseTypeSchema { get; set; }
        internal override string LogicalTypeName => LogicalTypeEnum.BigDecimal;

        internal static byte[] Encode(decimal value)
        {
            Span<int> bits = stackalloc int[4];
            decimal.GetBits(value, bits);
            var unscaled = new BigInteger((uint)bits[0])
                           | (new BigInteger((uint)bits[1]) << 32)
                           | (new BigInteger((uint)bits[2]) << 64);
            if (value < 0)
            {
                unscaled = -unscaled;
            }

            int scale = (bits[3] >> 16) & 0xFF;

            using var writer = new Writer();
            writer.WriteBytes(unscaled.ToByteArray(isUnsigned: false, isBigEndian: true));
            writer.WriteInt(scale);
            return writer.ToArray();
        }

        private static readonly BigInteger MaxDecimalMantissa = (BigInteger.One << 96) - 1;
        private const int MaxDecimalScale = 28;

        internal static decimal Decode(byte[] encoded)
        {
            var reader = new Reader(encoded);
            var unscaledBytes = reader.ReadBytes();
            var scale = reader.ReadInt();
            var unscaled = unscaledBytes.Length == 0 ? BigInteger.Zero : new BigInteger(unscaledBytes, isUnsigned: false, isBigEndian: true);
            return ToDecimal(unscaled, scale);
        }

        /// <summary>
        /// unscaled × 10^-scale as <see cref="decimal"/>. Negative scales (e.g. Java's <c>1E+3</c>) are expanded; scales or
        /// precisions beyond <see cref="decimal"/> are rounded half away from zero; values above its range throw.
        /// </summary>
        internal static decimal ToDecimal(BigInteger unscaled, int scale)
        {
            if (scale < 0)
            {
                unscaled *= BigInteger.Pow(10, -scale);
                scale = 0;
            }

            var negative = unscaled.Sign < 0;
            var magnitude = BigInteger.Abs(unscaled);

            if (scale > MaxDecimalScale)
            {
                magnitude = DivideRounded(magnitude, BigInteger.Pow(10, scale - MaxDecimalScale));
                scale = MaxDecimalScale;
            }

            while (magnitude > MaxDecimalMantissa && scale > 0)
            {
                magnitude = DivideRounded(magnitude, 10);
                scale--;
            }

            if (magnitude > MaxDecimalMantissa)
            {
                throw new OverflowException($"big-decimal value {(negative ? "-" : "")}{magnitude}E{-scale} is outside the range of System.Decimal");
            }

            Span<byte> bytes = stackalloc byte[12];
            magnitude.TryWriteBytes(bytes, out _, isUnsigned: true, isBigEndian: false);
            return new decimal(
                BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(0, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(4, 4)),
                BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(8, 4)),
                negative,
                (byte)scale);
        }

        private static BigInteger DivideRounded(BigInteger value, BigInteger divisor)
        {
            var quotient = BigInteger.DivRem(value, divisor, out var remainder);
            return remainder * 2 >= divisor ? quotient + 1 : quotient;
        }

        internal override object ConvertToLogicalValue(object baseValue, LogicalTypeSchema schema, Type readType)
        {
            var value = Decode((byte[])baseValue);
            var target = Nullable.GetUnderlyingType(readType) ?? readType;
            if (target == typeof(decimal) || target == typeof(object))
            {
                return value;
            }

            if (target == typeof(string))
            {
                return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return Convert.ChangeType(value, target, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
