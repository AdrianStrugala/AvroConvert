#region license
/**Copyright (c) 2022  Adrian Strugala
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

using System;
using System.Linq;
using System.Security.Principal;
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
