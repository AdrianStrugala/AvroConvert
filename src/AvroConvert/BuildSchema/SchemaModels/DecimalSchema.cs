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
using SolTechnology.Avro.Exceptions;

namespace SolTechnology.Avro.BuildSchema.SchemaModels
{

    internal sealed class DecimalSchema : LogicalTypeSchema
    {
        internal override Avro.Schema.Schema.Type Type => Avro.Schema.Schema.Type.Logical;

        internal override TypeSchema BaseTypeSchema { get; set; }
        internal int Precision { get; set; }
        internal int Scale { get; set; }

        internal override string LogicalTypeName => "decimal";
        internal override Dictionary<string, object> Properties { get; }


        //Default C# values
        public DecimalSchema(Type runtimeType) : this(runtimeType, 29, 14)
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

            Properties = new Dictionary<string, object>();
            Properties.Add("precision", Precision);
            Properties.Add("scale", Scale);
        }

  



        /// <inheritdoc/>
        // internal override void ValidateSchema(LogicalSchema schema)
        // {
        //     if (Schema.Schema.Type.Bytes != schema.BaseSchema.Tag && Schema.Schema.Type.Fixed != schema.BaseSchema.Tag)
        //         throw new AvroTypeException("'decimal' can only be used with an underlying bytes or fixed type");
        //
        //     var precisionVal = schema.GetProperty("precision");
        //
        //     if (string.IsNullOrEmpty(precisionVal))
        //         throw new AvroTypeException("'decimal' requires a 'precision' property");
        //
        //     var precision = int.Parse(precisionVal, CultureInfo.CurrentCulture);
        //
        //     if (precision <= 0)
        //         throw new AvroTypeException("'decimal' requires a 'precision' property that is greater than zero");
        //
        //     var scale = GetScalePropertyValueFromSchema(schema);
        //
        //     if (scale < 0 || scale > precision)
        //         throw new AvroTypeException("'decimal' requires a 'scale' property that is zero or less than or equal to 'precision'");
        // }
        //
        // /// <inheritdoc/>      
        // internal override object ConvertToBaseValue(object logicalValue, LogicalSchema schema)
        // {
        //     var decimalValue = (AvroDecimal)logicalValue;
        //     var logicalScale = GetScalePropertyValueFromSchema(schema);
        //     var scale = decimalValue.Scale;
        //
        //     if (scale != logicalScale)
        //         throw new ArgumentOutOfRangeException(nameof(logicalValue), $"The decimal value has a scale of {scale} which cannot be encoded against a logical 'decimal' with a scale of {logicalScale}");
        //
        //     var buffer = decimalValue.UnscaledValue.ToByteArray();
        //
        //     Array.Reverse(buffer);
        //
        //     return Schema.Schema.Type.Bytes == schema.BaseSchema.Tag
        //         ? (object)buffer
        //         : (object)new GenericFixed(
        //             (FixedSchema)schema.BaseSchema,
        //             GetDecimalFixedByteArray(buffer, ((FixedSchema)schema.BaseSchema).Size,
        //             decimalValue.Sign < 0 ? (byte)0xFF : (byte)0x00));
        // }
        //
        // /// <inheritdoc/>
        // internal override object ConvertToLogicalValue(object baseValue, LogicalSchema schema)
        // {
        //     var buffer = Schema.Schema.Type.Bytes == schema.BaseSchema.Tag
        //         ? (byte[])baseValue
        //         : ((GenericFixed)baseValue).Value;
        //
        //     Array.Reverse(buffer);
        //
        //     return new AvroDecimal(new BigInteger(buffer), GetScalePropertyValueFromSchema(schema));
        // }
        //
        // /// <inheritdoc/>
        // internal override Type GetCSharpType(bool nullible)
        // {
        //     return nullible ? typeof(AvroDecimal?) : typeof(AvroDecimal);
        // }
        //
        // /// <inheritdoc/>
        // internal override bool IsInstanceOfLogicalType(object logicalValue)
        // {
        //     return logicalValue is AvroDecimal;
        // }
        //
        // private static int GetScalePropertyValueFromSchema(Schema.Schema schema, int defaultVal = 0)
        // {
        //     var scaleVal = schema.GetProperty("scale");
        //
        //     return string.IsNullOrEmpty(scaleVal) ? defaultVal : int.Parse(scaleVal, CultureInfo.CurrentCulture);
        // }
        //
        // private static byte[] GetDecimalFixedByteArray(byte[] sourceBuffer, int size, byte fillValue)
        // {
        //     var paddedBuffer = new byte[size];
        //
        //     var offset = size - sourceBuffer.Length;
        //
        //     for (var idx = 0; idx < size; idx++)
        //     {
        //         paddedBuffer[idx] = idx < offset ? fillValue : sourceBuffer[idx - offset];
        //     }
        //
        //     return paddedBuffer;
        // }


    }
}
