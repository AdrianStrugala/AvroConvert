#region license
/**Copyright (c) 2021 Adrian Struga³a
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
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Avro.Generic;
using Newtonsoft.Json;
using SolTechnology.Avro.BuildSchema.SchemaModels.Abstract;
using SolTechnology.Avro.Exceptions;
using NamedSchema = SolTechnology.Avro.BuildSchema.SchemaModels.Abstract.NamedSchema;

namespace SolTechnology.Avro.BuildSchema.SchemaModels
{

    internal sealed class DecimalSchema : LogicalTypeSchema
    {
        internal override Avro.Schema.Schema.Type Type => Avro.Schema.Schema.Type.Logical;

        internal override TypeSchema BaseTypeSchema { get; set; }
        internal int Precision { get; set; }
        internal int Scale { get; set; }

        internal override string LogicalTypeName => "decimal";

        public DecimalSchema() : this(typeof(decimal))
        {
        }
        public DecimalSchema(Type runtimeType) : this(runtimeType, 29, 14)  //Default C# values
        {
        }

        public DecimalSchema(Type runtimeType, int precision, int scale) : base(runtimeType)
        {
            BaseTypeSchema = new BytesSchema();

            if (precision <= 0)
                throw new AvroTypeException("Property [Precision] of [Decimal] schema has to be greater than 0");

            if (scale < 0)
                throw new AvroTypeException("Property [Scale] of [Decimal] schema has to be greater equal 0");

            if (scale > precision)
                throw new AvroTypeException("Property [Scale] of [Decimal] schema has to be lesser equal [Precision]");

            Scale = scale;
            Precision = precision;
        }

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            writer.WriteStartObject();
            writer.WriteProperty("type", BaseTypeSchema.Type.ToString().ToLowerInvariant());
            writer.WriteProperty("logicalType", LogicalTypeName);
            writer.WriteProperty("precision", Precision);
            writer.WriteProperty("scale", Scale);
            writer.WriteEndObject();
        }

        internal object ConvertToBaseValue(object logicalValue, DecimalSchema schema)
        {
            var decimalValue = new AvroDecimal((decimal) logicalValue);
            var logicalScale = Scale;
            var scale = decimalValue.Scale;

            if (scale != logicalScale)
            {
                //Resize
                int sizeDiff = logicalScale - scale;
                string trailingZeros = new string('0', sizeDiff);
                var resizedDecimal = decimal.Parse($"{logicalValue}{trailingZeros}");

                decimalValue = new AvroDecimal(resizedDecimal);
            }
                
        
            var buffer = decimalValue.UnscaledValue.ToByteArray();
        
            Array.Reverse(buffer);
        
            return Avro.Schema.Schema.Type.Bytes == schema.BaseTypeSchema.Type
                ? (object)buffer
                : (object)new FixedModel(
                    (FixedSchema)schema.BaseTypeSchema,
                    GetDecimalFixedByteArray(buffer, ((FixedSchema)schema.BaseTypeSchema).Size,
                    decimalValue.Sign < 0 ? (byte)0xFF : (byte)0x00));
        }

        internal override object ConvertToLogicalValue(object baseValue, LogicalTypeSchema schema, Type type)
        {
            var buffer = Avro.Schema.Schema.Type.Bytes == schema.BaseTypeSchema.Type
                ? (byte[])baseValue
                : ((GenericFixed)baseValue).Value;

            Array.Reverse(buffer);
            var avroDecimal = new AvroDecimal(new BigInteger(buffer), Scale);
            return AvroDecimal.ToDecimal(avroDecimal);
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
