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
using SolTechnology.Avro.Extensions;
using SolTechnology.Avro.Schema.Abstract;

namespace SolTechnology.Avro.Schema
{

    internal sealed class TimestampMicrosecondsSchema : LogicalTypeSchema
    {
        public TimestampMicrosecondsSchema() : this(typeof(DateTime))
        {
        }
        public TimestampMicrosecondsSchema(Type runtimeType) : base(runtimeType)
        {
            BaseTypeSchema = new LongSchema();
        }

        internal override AvroType Type => Avro.Schema.AvroType.Logical;
        internal override TypeSchema BaseTypeSchema { get; set; }
        internal override string LogicalTypeName => LogicalTypeEnum.TimeMicrosecond;
        internal object ConvertToBaseValue(object logicalValue, TimestampMillisecondsSchema schema)
        {
            DateTime date;
            if (logicalValue is DateTimeOffset dateTimeOffset)
            {
                date = dateTimeOffset.DateTime;
            }
            else
            {
                date = ((DateTime)logicalValue);
            }

            var timeDiff = date - DateTimeExtensions.UnixEpochDateTime;
            return timeDiff.Milliseconds / 1000;
        }

        internal override object ConvertToLogicalValue(object baseValue, LogicalTypeSchema schema, Type type)
        {
            var noMicroseconds = (long)baseValue;
            var result = DateTimeExtensions.UnixEpochDateTime.AddMilliseconds(noMicroseconds * 1000);

            if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(noMicroseconds * 1000);
            }
            else
            {
                return result;
            }
        }
    }
}
