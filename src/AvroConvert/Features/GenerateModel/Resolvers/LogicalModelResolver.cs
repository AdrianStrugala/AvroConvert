#region license
/**Copyright (c) 2021 Adrian Strugala
*
* Licensed under the CC BY-NC-SA 3.0 License(the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://creativecommons.org/licenses/by-nc-sa/3.0/
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* You are free to use or modify the code for personal usage.
* For commercial usage purchase the product at
*
* https://xabe.net/product/avroconvert/
*/
#endregion

using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.GenerateModel.Resolvers
{
    internal class LogicalModelResolver
    {
        internal string ResolveLogical(JObject typeObj)
        {
            string logicalType = typeObj["logicalType"].ToString();

            switch (logicalType)
            {
                case LogicalTypeSchema.LogicalTypeEnum.Date:
                case LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds:
                case LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds:
                    return "DateTime";
                case LogicalTypeSchema.LogicalTypeEnum.Decimal:
                    return "decimal";
                case LogicalTypeSchema.LogicalTypeEnum.Duration:
                case LogicalTypeSchema.LogicalTypeEnum.TimeMicrosecond:
                case LogicalTypeSchema.LogicalTypeEnum.TimeMilliseconds:
                    return "TimeSpan";
                case LogicalTypeSchema.LogicalTypeEnum.Uuid:
                    return "Guid";
                default:
                    throw new InvalidAvroObjectException($"Unidentified logicalType {logicalType}");
            }
        }
    }
}
