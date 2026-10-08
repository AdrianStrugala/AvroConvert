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
using SolTechnology.Avro.Features.GenerateModel.NetModel;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.GenerateModel.Resolvers
{
    internal class ArrayModelResolver
    {
        internal NetClassField ResolveArray(JObject typeObj)
        {
            var avroField = new NetClassField();

            // If this is an array of a specific class that's being defined in this area of the json
            if (typeObj["items"] is JObject && ((JObject)typeObj["items"])["type"].ToString() == "record")
            {
                avroField.FieldType = ((JObject)typeObj["items"])["name"] + "[]";
                avroField.Namespace = ((JObject)typeObj["items"])["namespace"]?.ToString();
            }
            else if (typeObj["items"] is JValue value)
            {
                avroField.FieldType = value + "[]";
            }
            else
            {
                throw new InvalidAvroObjectException($"{typeObj}");
            }

            return avroField;
        }
    }
}
