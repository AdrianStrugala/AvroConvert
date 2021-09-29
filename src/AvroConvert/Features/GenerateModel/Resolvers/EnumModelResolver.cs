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
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.Features.GenerateModel.Models;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.GenerateModel.Resolvers
{
    internal class EnumModelResolver
    {
        internal void ResolveEnum(JToken propValue, AvroModel model)
        {
            var result = new AvroEnum();

            var name = propValue["name"].ToString().Split('.').Last();
            var symbols = (JArray)propValue["symbols"];

            result.EnumName = name;
            result.Symbols = symbols.Select(s => s.ToString()).ToList();

            model.Enums.Add(result);
        }
    }
}
