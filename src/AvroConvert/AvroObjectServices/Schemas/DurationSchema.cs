#region license
/**Copyright (c) 2021 Adrian Struga�a
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
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.Schemas
{

    internal sealed class DurationSchema : LogicalTypeSchema
    {
        public DurationSchema() : this(typeof(TimeSpan))
        {
        }
        public DurationSchema(Type runtimeType) : base(runtimeType)
        {
            BaseTypeSchema = new FixedSchema(
                new NamedEntityAttributes(new SchemaName("duration"), new List<string>(), ""),
                12,
                typeof(TimeSpan));
        }

        internal override AvroType Type => AvroType.Logical;
        internal override TypeSchema BaseTypeSchema { get; set; }
        internal override string LogicalTypeName => "duration";

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            var baseSchema = (FixedSchema)BaseTypeSchema;
            writer.WriteStartObject();
            writer.WriteProperty("type", baseSchema.Type.ToString().ToLowerInvariant());
            writer.WriteProperty("size", baseSchema.Size);
            writer.WriteProperty("name", baseSchema.Name);
            writer.WriteProperty("logicalType", LogicalTypeName);
            writer.WriteEndObject();
        }

        internal override object ConvertToLogicalValue(object baseValue, LogicalTypeSchema schema, Type readType)
        {
            byte[] baseBytes = (byte[])baseValue;
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(baseBytes); //reverse it so we get big endian.

            int months = BitConverter.ToInt32(baseBytes.Skip(0).Take(4).ToArray(), 0);
            int days = BitConverter.ToInt32(baseBytes.Skip(4).Take(4).ToArray(), 0);
            int milliseconds = BitConverter.ToInt32(baseBytes.Skip(8).Take(4).ToArray(), 0);

            var result = new TimeSpan(months * 30 + days, 0, 0, 0, milliseconds);

            if (readType == typeof(TimeOnly) || readType == typeof(TimeOnly?))
            {
                return TimeOnly.FromTimeSpan(result);
            }

            return result;
        }
    }
}
