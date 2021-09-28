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

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.GenerateModel.Resolvers
{
    internal class ArrayModelResolver
    {
        internal string ResolveArray(JObject typeObj)
        {
            string fieldType;

            // If this is an array of a specific class that's being defined in this area of the json
            if (typeObj["items"] is JObject && ((JObject)typeObj["items"])["type"].ToString() == "record")
            {
                fieldType = ((JObject)typeObj["items"])["name"] + "[]";
            }
            else if (typeObj["items"] is JValue value)
            {
                fieldType = value + "[]";
            }
            else
            {
                throw new InvalidAvroObjectException($"{typeObj}");
            }

            return fieldType;
        }
    }
}
