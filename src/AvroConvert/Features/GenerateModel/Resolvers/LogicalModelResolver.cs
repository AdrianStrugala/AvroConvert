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
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace SolTechnology.Avro.Features.GenerateModel.Resolvers
{
    internal class LogicalModelResolver
    {
        private const int DefaultDecimalPrecision = 29;
        private const int DefaultDecimalScale = 14;

        /// <summary>
        /// .NET type of a logical type, and the attribute that is needed when the default mapping of that type would give
        /// another schema (e.g. DateTime is timestamp-micros by default, so timestamp-millis needs [AvroType]).
        /// </summary>
        internal (string Type, string Attribute) ResolveLogical(JObject typeObj)
        {
            string logicalType = typeObj["logicalType"].ToString();
            string baseType = typeObj["type"]?.ToString();

            switch (logicalType)
            {
                case LogicalTypeSchema.LogicalTypeEnum.Date:
                    return ("DateOnly", null);
                case LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds:
                    return ("DateTime", null);
                case LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds:
                    return ("DateTime", AvroType("TimestampMilliseconds"));
                case LogicalTypeSchema.LogicalTypeEnum.TimestampNanoseconds:
                    return ("DateTime", AvroType("TimestampNanoseconds"));
                case LogicalTypeSchema.LogicalTypeEnum.LocalTimestampMilliseconds:
                    return ("DateTime", AvroType("LocalTimestampMilliseconds"));
                case LogicalTypeSchema.LogicalTypeEnum.LocalTimestampMicroseconds:
                    return ("DateTime", AvroType("LocalTimestampMicroseconds"));
                case LogicalTypeSchema.LogicalTypeEnum.LocalTimestampNanoseconds:
                    return ("DateTime", AvroType("LocalTimestampNanoseconds"));
                case LogicalTypeSchema.LogicalTypeEnum.Decimal:
                    {
                        int precision = typeObj["precision"]?.Value<int>() ?? DefaultDecimalPrecision;
                        int scale = typeObj["scale"]?.Value<int>() ?? 0;
                        return precision == DefaultDecimalPrecision && scale == DefaultDecimalScale
                            ? ("decimal", null)
                            : ("decimal", $"[AvroDecimal(Precision = {precision}, Scale = {scale})]");
                    }
                case LogicalTypeSchema.LogicalTypeEnum.BigDecimal:
                    return ("decimal", AvroType("BigDecimal"));
                case LogicalTypeSchema.LogicalTypeEnum.Duration:
                    return ("TimeSpan", null);
                case LogicalTypeSchema.LogicalTypeEnum.TimeMicrosecond:
                    return ("TimeOnly", null);
                case LogicalTypeSchema.LogicalTypeEnum.TimeMilliseconds:
                    return ("TimeOnly", AvroType("TimeMilliseconds"));
                case LogicalTypeSchema.LogicalTypeEnum.Uuid:
                    return baseType == "fixed" ? ("Guid", AvroType("UuidFixed")) : ("Guid", null);
                default:
                    return (null, null);
            }
        }

        private static string AvroType(string representation) => $"[AvroType(AvroTypeRepresentation.{representation})]";
    }
}
