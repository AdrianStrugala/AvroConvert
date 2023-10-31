#region license
/**Copyright (c) 2021 Adrian Strugala
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#endregion

using System;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.AvroTypes;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;

// ReSharper disable once CheckNamespace
namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal partial class WriteResolver
    {
        internal void ResolveDecimal(DecimalSchema schema, object logicalValue, IWriter writer)
        {
            var avroDecimal = new AvroDecimal((decimal)logicalValue);
            var logicalScale = schema.Scale;
            var scale = avroDecimal.Scale;

            //Resize value to match schema Scale property
            int sizeDiff = logicalScale - scale;
            if (sizeDiff < 0)
            {
                throw new AvroTypeException(
                    $@"Decimal Scale for value [{logicalValue}] is equal to [{scale}]. This exceeds default setting [{logicalScale}].
Consider adding following attribute to your property:
[AvroDecimal(Precision = 28, Scale = {scale})]
");
            }

            string trailingZeros = new string('0', sizeDiff);
            var logicalValueString = logicalValue.ToString();

            string valueWithTrailingZeros;
#if NET6_0_OR_GREATER
            if (logicalValueString.Contains(avroDecimal.SeparatorCharacter))
#else
            if (logicalValueString.Contains(avroDecimal.SeparatorCharacter.ToString()))
#endif
            {
                valueWithTrailingZeros = $"{logicalValueString}{trailingZeros}";
            }
            else
            {
                valueWithTrailingZeros = $"{logicalValueString}{avroDecimal.SeparatorCharacter}{trailingZeros}";
            }

            avroDecimal = new AvroDecimal(valueWithTrailingZeros);

#if NET6_0_OR_GREATER
            Span<byte> buffer = stackalloc byte[avroDecimal.UnscaledValue.GetByteCount(isUnsigned: false)];
            avroDecimal.UnscaledValue.TryWriteBytes(buffer, out var _);
            buffer.Reverse();
#else            
            var buffer = new Span<byte>(avroDecimal.UnscaledValue.ToByteArray());
            buffer.Reverse();
#endif
            var result = AvroType.Bytes == schema.BaseTypeSchema.Type
                ? buffer
                : new AvroFixed(
                    (FixedSchema)schema.BaseTypeSchema,
                    GetDecimalFixedByteArray(ref buffer, ((FixedSchema)schema.BaseTypeSchema).Size,
                        avroDecimal.Sign < 0 ? (byte)0xFF : (byte)0x00))
                    .Value.AsSpan();

            writer.WriteBytes(result);
        }

        private static byte[] GetDecimalFixedByteArray(
            ref Span<byte> sourceBuffer,
            int size,
            byte fillValue)
        {
            var offset = size - sourceBuffer.Length;

            var paddedBuffer = new byte[size];
            paddedBuffer.AsSpan(0, offset).Fill(fillValue);

            for (var idx = offset; idx < size; idx++)
            {
                paddedBuffer[idx] = sourceBuffer[idx - offset];
            }

            return paddedBuffer;
        }
    }
}
