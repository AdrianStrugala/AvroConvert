/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.Util
{
    /// <summary>
    /// The 'timestamp-millis' logical type.
    /// </summary>
    internal class TimestampMillisecond : LogicalUnixEpochType<DateTime>
    {
        /// <summary>
        /// The logical type name for TimestampMillisecond.
        /// </summary>
        internal static readonly string LogicalTypeName = "timestamp-millis";

        /// <summary>
        /// Initializes a new TimestampMillisecond logical type.
        /// </summary>
        internal TimestampMillisecond() : base(LogicalTypeName)
        { }

        /// <inheritdoc/>
        internal override void ValidateSchema(LogicalSchema schema)
        {
            if (Schema.Schema.Type.Long != schema.BaseSchema.Tag)
                throw new AvroTypeException("'timestamp-millis' can only be used with an underlying long type");
        }

        /// <inheritdoc/>
        internal override object ConvertToBaseValue(object logicalValue, LogicalSchema schema)
        {
            var date = ((DateTime)logicalValue).ToUniversalTime();
            return (long)(date - UnixEpochDateTime).TotalMilliseconds;
        }

        /// <inheritdoc/>
        internal override object ConvertToLogicalValue(object baseValue, LogicalSchema schema)
        {
            var noMs = (long)baseValue;
            return UnixEpochDateTime.AddMilliseconds(noMs);
        }
    }
}
