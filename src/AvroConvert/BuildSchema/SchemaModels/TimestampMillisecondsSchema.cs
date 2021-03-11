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
using SolTechnology.Avro.BuildSchema.SchemaModels.Abstract;
using SolTechnology.Avro.Extensions;

namespace SolTechnology.Avro.BuildSchema.SchemaModels
{

    internal sealed class TimestampMillisecondsSchema : LogicalTypeSchema
    {
        public TimestampMillisecondsSchema(Type runtimeType) : base(runtimeType)
        {
            BaseTypeSchema = new LongSchema();
        }

        internal override Avro.Schema.Schema.Type Type => Avro.Schema.Schema.Type.Logical;
        internal override TypeSchema BaseTypeSchema { get; set; }
        internal override string LogicalTypeName => "timestamp-millis";

        public object ConvertToBaseValue(object logicalValue, TimestampMillisecondsSchema schema)
        {
            DateTime date;
            if (logicalValue is DateTimeOffset dateTimeOffset)
            {
                date = dateTimeOffset.DateTime;
            }
            else
            {
                date = ((DateTime)logicalValue).ToUniversalTime();
            }

            return (long)(date - DateTimeExtensions.UnixEpochDateTime).TotalMilliseconds;
        }

        public object ConvertToLogicalValue(object baseValue, TimestampMillisecondsSchema schema)
        {
            var noMs = (long)baseValue;
            return DateTimeExtensions.UnixEpochDateTime.AddMilliseconds(noMs);
        }
    }
}
