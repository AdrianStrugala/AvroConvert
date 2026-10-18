#region license
/**Copyright (c) 2019-2026 Adrian Strugała (SolTechnology)
*
* Licensed under the PolyForm Noncommercial License 1.0.0, the PolyForm Small Business License 1.0.0,
* or the SolTechnology Commercial Licence – pick the one that applies to you. See LICENSE.md:
*
* https://github.com/AdrianStrugala/AvroConvert/blob/master/LICENSE.md
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*
* Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)
*/
#endregion

using System;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.Schemas
{
    /// <summary>
    /// <c>timestamp-nanos</c> and <c>local-timestamp-{millis,micros,nanos}</c> (Avro 1.12). A <c>long</c> counting units from
    /// 1970-01-01T00:00:00; for <c>timestamp-*</c> the instant is UTC, for <c>local-timestamp-*</c> it is a wall-clock time
    /// with no time zone (read back as <see cref="DateTimeKind.Unspecified"/>).
    /// </summary>
    internal sealed class TimestampSchema : LogicalTypeSchema
    {
        internal TimestampSchema(string logicalTypeName) : this(logicalTypeName, typeof(DateTime))
        {
        }

        internal TimestampSchema(string logicalTypeName, Type runtimeType) : base(runtimeType)
        {
            switch (logicalTypeName)
            {
                case LogicalTypeEnum.TimestampNanoseconds:
                    IsLocal = false; NanosecondsPerUnit = 1;
                    break;
                case LogicalTypeEnum.LocalTimestampMilliseconds:
                    IsLocal = true; NanosecondsPerUnit = 1_000_000;
                    break;
                case LogicalTypeEnum.LocalTimestampMicroseconds:
                    IsLocal = true; NanosecondsPerUnit = 1_000;
                    break;
                case LogicalTypeEnum.LocalTimestampNanoseconds:
                    IsLocal = true; NanosecondsPerUnit = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logicalTypeName), logicalTypeName, "Not a timestamp logical type handled by TimestampSchema");
            }

            LogicalTypeName = logicalTypeName;
            BaseTypeSchema = new LongSchema();
        }

        internal override AvroType Type => AvroType.Logical;
        internal override TypeSchema BaseTypeSchema { get; set; }
        internal override string LogicalTypeName { get; }

        /// <summary>True for <c>local-timestamp-*</c>.</summary>
        internal bool IsLocal { get; }

        internal long NanosecondsPerUnit { get; }

        internal static bool Handles(string logicalTypeName) =>
            logicalTypeName is LogicalTypeEnum.TimestampNanoseconds
                or LogicalTypeEnum.LocalTimestampMilliseconds
                or LogicalTypeEnum.LocalTimestampMicroseconds
                or LogicalTypeEnum.LocalTimestampNanoseconds;

        /// <summary>Encoded value for a <see cref="DateTime"/> tick count (100 ns) since the epoch.</summary>
        internal long FromTicks(long ticksSinceEpoch)
        {
            if (NanosecondsPerUnit >= 100)
            {
                return ticksSinceEpoch / (NanosecondsPerUnit / 100);
            }

            try
            {
                return checked(ticksSinceEpoch * (100 / NanosecondsPerUnit));
            }
            catch (OverflowException)
            {
                // A long holds nanoseconds from 1677-09-21 to 2262-04-11 only.
                throw new ArgumentOutOfRangeException(nameof(ticksSinceEpoch),
                    $"{new DateTime(DateTimeExtensions.UnixEpochDateTime.Ticks + ticksSinceEpoch):O} is outside the range of '{LogicalTypeName}' (1677-09-21 to 2262-04-11).");
            }
        }

        internal long ToTicks(long value) =>
            NanosecondsPerUnit >= 100
                ? checked(value * (NanosecondsPerUnit / 100))
                : value / (100 / NanosecondsPerUnit);

        internal override object ConvertToLogicalValue(object baseValue, LogicalTypeSchema schema, Type readType)
        {
            if (readType == typeof(long) || readType == typeof(long?))
            {
                // The raw count since the epoch: exact for any value, including nanoseconds below DateTime's 100 ns resolution.
                return baseValue;
            }

            var ticks = ToTicks((long)baseValue);
            var kind = IsLocal ? DateTimeKind.Unspecified : DateTimeKind.Utc;
            var dateTime = new DateTime(DateTimeExtensions.UnixEpochDateTime.Ticks + ticks, kind);

            if (readType == typeof(DateTimeOffset) || readType == typeof(DateTimeOffset?))
            {
                return new DateTimeOffset(dateTime.Ticks, TimeSpan.Zero);
            }

            if (readType == typeof(DateOnly) || readType == typeof(DateOnly?))
            {
                return DateOnly.FromDateTime(dateTime);
            }

            return dateTime;
        }
    }
}
