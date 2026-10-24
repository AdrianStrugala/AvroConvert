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

namespace SolTechnology.Avro.Infrastructure.Extensions
{
    internal static class DateTimeExtensions
    {
        internal static DateTime UnixEpochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        internal static DateOnly UnixEpochDate = new DateOnly(1970, 1, 1);
        internal static TimeOnly UnixEpochTime = new TimeOnly(0, 0, 0);
    }
}
