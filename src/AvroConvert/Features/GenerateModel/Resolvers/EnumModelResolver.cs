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
using System.Linq;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.Features.GenerateModel.NetModel;

namespace SolTechnology.Avro.Features.GenerateModel.Resolvers
{
    internal class EnumModelResolver
    {
        internal void ResolveEnum(JToken propValue, NetModel.NetModel model)
        {
            var result = new NetEnum();

            var name = propValue["name"].ToString().Split('.').Last();
            var symbols = (JArray)propValue["symbols"];

            result.Name = name;
            result.Symbols = symbols.Select(s => s.ToString()).ToList();

            model.NetTypes.Add(result);
        }
    }
}
