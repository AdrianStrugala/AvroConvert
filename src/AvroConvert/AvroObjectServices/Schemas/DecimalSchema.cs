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
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Schemas.AvroTypes;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;
using NamedSchema = SolTechnology.Avro.AvroObjectServices.Schemas.Abstract.NamedSchema;

namespace SolTechnology.Avro.AvroObjectServices.Schemas
{

    internal sealed class DecimalSchema : LogicalTypeSchema
    {
        internal override AvroType Type => AvroType.Logical;

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

        internal override object ConvertToLogicalValue(object baseValue, LogicalTypeSchema schema, Type readType)
        {
            var buffer = AvroType.Bytes == schema.BaseTypeSchema.Type
                ? (byte[])baseValue
                : ((AvroFixed)baseValue).Value;

            Array.Reverse(buffer);
            var avroDecimal = new AvroDecimal(new BigInteger(buffer), Scale);


            var value = AvroDecimal.ToDecimal(avroDecimal);

            if (readType != typeof(decimal))
            {
                if (readType == typeof(int))
                {
                    return Convert.ToInt32(value);
                }

                if (readType == typeof(short))
                {
                    return Convert.ToInt16(value);
                }

                if (readType == typeof(double))
                {
                    return Convert.ToDouble(value);
                }


                if (readType == typeof(long))
                {
                    return Convert.ToInt64(value);
                }

                if (readType == typeof(float))
                {
                    return Convert.ToSingle(value);
                }
            }

            return value;
        }



    }
}
