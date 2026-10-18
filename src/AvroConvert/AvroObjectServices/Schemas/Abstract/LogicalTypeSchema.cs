#region license
/**Copyright (c) 2020 Adrian Struga�a
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
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.Schemas.Abstract
{
    internal abstract class LogicalTypeSchema : TypeSchema
    {
        internal class LogicalTypeEnum
        {
            internal const string
                Uuid = "uuid",
                TimestampMilliseconds = "timestamp-millis",
                TimestampMicroseconds = "timestamp-micros",
                TimestampNanoseconds = "timestamp-nanos",
                LocalTimestampMilliseconds = "local-timestamp-millis",
                LocalTimestampMicroseconds = "local-timestamp-micros",
                LocalTimestampNanoseconds = "local-timestamp-nanos",
                Decimal = "decimal",
                BigDecimal = "big-decimal",
                Duration = "duration",
                TimeMilliseconds = "time-millis",
                TimeMicrosecond = "time-micros",
                Date = "date";
        }

        internal abstract TypeSchema BaseTypeSchema { get; set; }
        internal abstract string LogicalTypeName { get; }


        protected LogicalTypeSchema(Type runtimeType) : base(runtimeType, new Dictionary<string, string>())
        {
        }

        internal override bool CanRead(TypeSchema writerSchema)
        {
            return writerSchema.Type == Type || writerSchema.Type == BaseTypeSchema.Type;
        }

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            if (BaseTypeSchema is FixedSchema fixedBase)
            {
                // A named fixed is defined once per effective full name (a fixed without namespace takes the enclosing one);
                // later occurrences reference that full name, which carries the logical type with it.
                if (seenSchemas is SchemaWriteContext context)
                {
                    var fullName = context.EffectiveFullName(fixedBase);
                    if (!context.TryDefine(fullName))
                    {
                        writer.WriteValue(fullName);
                        return;
                    }
                }

                writer.WriteStartObject();
                writer.WriteProperty("type", "fixed");
                writer.WriteProperty("size", fixedBase.Size);
                writer.WriteProperty("name", fixedBase.Name);
                writer.WriteOptionalProperty("namespace", fixedBase.Namespace);
                writer.WriteOptionalProperty("aliases", fixedBase.Aliases);
                writer.WriteProperty("logicalType", LogicalTypeName);
                WriteLogicalProperties(writer);
                writer.WriteEndObject();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("type");
            BaseTypeSchema.ToJsonSafe(writer, seenSchemas);
            writer.WriteProperty("logicalType", LogicalTypeName);
            WriteLogicalProperties(writer);
            writer.WriteEndObject();
        }

        /// <summary>Logical-type specific attributes (e.g. precision and scale) written after <c>logicalType</c>.</summary>
        protected virtual void WriteLogicalProperties(JsonTextWriter writer)
        {
        }

        internal abstract object ConvertToLogicalValue(object baseValue, LogicalTypeSchema schema, Type readType);
    }
}