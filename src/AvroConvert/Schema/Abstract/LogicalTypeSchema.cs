#region license
/**Copyright (c) 2020 Adrian Struga³a
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
using Newtonsoft.Json;
using SolTechnology.Avro.BuildSchema;

namespace SolTechnology.Avro.Schema.Abstract
{
    internal abstract class LogicalTypeSchema : TypeSchema
    {
        internal class LogicalTypeEnum
        {
            internal const string
                Uuid = "uuid",
                TimestampMilliseconds = "timestamp-millis",
                TimestampMicroseconds = "timestamp-micros",
                Decimal = "decimal",
                Duration = "duration",
                TimeMilliseconds = "time-millis",
                TimeMicrosecond = "time-micros ",
                Date = "date";
        }

        internal abstract TypeSchema BaseTypeSchema { get; set; }
        internal abstract string LogicalTypeName { get; }

     
        protected LogicalTypeSchema(Type runtimeType) : base(runtimeType, new Dictionary<string, string>())
        {
        }

        internal override void ToJsonSafe(JsonTextWriter writer, HashSet<NamedSchema> seenSchemas)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            BaseTypeSchema.ToJsonSafe(writer, seenSchemas);
            writer.WriteProperty("logicalType", LogicalTypeName);
            writer.WriteEndObject();
        }

        internal abstract object ConvertToLogicalValue(object baseValue, LogicalTypeSchema schema, Type type);
    }
}