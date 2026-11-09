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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;

namespace SolTechnology.Avro.Infrastructure
{
    /// <summary>
    /// Newtonsoft with its own defaults, for every JSON the library reads or writes. JsonConvert and JToken.ToObject apply
    /// JsonConvert.DefaultSettings of the application, so a converter, a contract resolver or a date setting meant for the
    /// application's own types changed how Avro data and schema defaults were read and written.
    /// </summary>
    [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
    internal static class Json
    {
        private static JsonSerializer _serializer;

        /// <summary>Not changed after it is created, so it can be shared. Created on first use: a static initializer is not
        /// covered by the attributes above. Two threads may each create one at first; either works.</summary>
        internal static JsonSerializer Serializer => _serializer ??= JsonSerializer.Create(new JsonSerializerSettings { CheckAdditionalContent = true });

        /// <summary>As JsonConvert.DeserializeObject&lt;T&gt;, without the application's settings.</summary>
        internal static T Deserialize<T>(string json)
        {
            using var reader = new JsonTextReader(new StringReader(json));
            return Serializer.Deserialize<T>(reader);
        }
    }
}
