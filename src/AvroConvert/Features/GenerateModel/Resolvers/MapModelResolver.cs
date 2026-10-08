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
using System.Linq;
using Newtonsoft.Json.Linq;

namespace SolTechnology.Avro.Features.GenerateModel.Resolvers
{
    internal class MapModelResolver
    {
        internal string ResolveMap(JObject typeObj)
        {
            string valueTypeString;
            var valueType = typeObj["values"];

            if (valueType is JArray)
            {
                if (valueType.Count() == 2
                    && string.Equals(valueType[0].ToString(), "Null", StringComparison.InvariantCultureIgnoreCase))
                {
                    valueTypeString = valueType[1] + "?";
                }
                else
                {
                    valueTypeString = "object";
                }
            }
            else
            {
                valueTypeString = valueType.ToString();
            }


            return $"Dictionary<string,{valueTypeString}>";

        }
    }
}
