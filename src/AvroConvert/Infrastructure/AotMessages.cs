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
namespace SolTechnology.Avro.Infrastructure
{
    /// <summary>Messages for the trimming / Native AOT annotations of the reflection-based API.</summary>
    internal static class AotMessages
    {
        internal const string Reflection =
            "AvroConvert maps .NET types to Avro by reflection; members of those types may be removed by trimming.";

        internal const string DynamicCode =
            "AvroConvert compiles serializers at runtime (Expression.Compile, MakeGenericType), which Native AOT does not support.";
    }
}
