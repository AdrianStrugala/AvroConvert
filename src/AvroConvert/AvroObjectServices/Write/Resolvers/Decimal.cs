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

using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.AvroTypes;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Write.Resolvers
{
    internal class Decimal
    {
        internal void Resolve(DecimalSchema schema, object logicalValue, IWriter writer)
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
            if (logicalValueString.Contains(avroDecimal.SeparatorCharacter.ToString()))
            {
                valueWithTrailingZeros = $"{logicalValue}{trailingZeros}";
            }
            else
            {
                valueWithTrailingZeros = $"{logicalValue}{avroDecimal.SeparatorCharacter}{trailingZeros}";
            }

            avroDecimal = new AvroDecimal(valueWithTrailingZeros);

            var buffer = avroDecimal.UnscaledValue.ToByteArray();
            System.Array.Reverse(buffer);

            var result = AvroType.Bytes == schema.BaseTypeSchema.Type
                ? (object)buffer
                : (object)new AvroFixed(
                    (FixedSchema)schema.BaseTypeSchema,
                    GetDecimalFixedByteArray(buffer, ((FixedSchema)schema.BaseTypeSchema).Size,
                        avroDecimal.Sign < 0 ? (byte)0xFF : (byte)0x00));

            writer.WriteBytes((byte[])result);
        }


        private static byte[] GetDecimalFixedByteArray(byte[] sourceBuffer, int size, byte fillValue)
        {
            var paddedBuffer = new byte[size];

            var offset = size - sourceBuffer.Length;

            for (var idx = 0; idx < size; idx++)
            {
                paddedBuffer[idx] = idx < offset ? fillValue : sourceBuffer[idx - offset];
            }

            return paddedBuffer;
        }
    }
}
