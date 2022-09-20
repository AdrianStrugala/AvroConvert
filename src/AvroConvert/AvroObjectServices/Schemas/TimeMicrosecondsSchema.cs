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
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.Schemas
{

    internal sealed class TimeMicrosecondsSchema : LogicalTypeSchema
    {
        private static readonly TimeSpan _maxTime = new TimeSpan(23, 59, 59);

        public TimeMicrosecondsSchema() : this(typeof(TimeSpan))
        {
        }
        public TimeMicrosecondsSchema(Type runtimeType) : base(runtimeType)
        {
            BaseTypeSchema = new LongSchema();
        }

        internal override AvroType Type => AvroType.Logical;
        internal override TypeSchema BaseTypeSchema { get; set; }
        internal override string LogicalTypeName => LogicalTypeEnum.TimeMicrosecond;
        public object ConvertToBaseValue(object logicalValue, LogicalTypeSchema schema)
        {
            var time = (TimeSpan)logicalValue;

            if (time > _maxTime)
                throw new ArgumentOutOfRangeException(nameof(logicalValue), "A 'time-micros' value can only have the range '00:00:00' to '23:59:59'.");

            return (long)(time - DateTimeExtensions.UnixEpochDateTime.TimeOfDay).TotalMilliseconds * 1000;
        }

        internal override object ConvertToLogicalValue(object baseValue, LogicalTypeSchema schema, Type readType)
        {
            var noMs = (long)baseValue / 1000;
            return DateTimeExtensions.UnixEpochDateTime.TimeOfDay.Add(TimeSpan.FromMilliseconds(noMs));
        }
    }
}
