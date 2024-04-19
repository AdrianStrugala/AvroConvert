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
using System.Linq;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.Features.GenerateModel.NetModel;

namespace SolTechnology.Avro.Features.GenerateModel
{
    internal class NamespaceHelper
    {
        internal void EnsureUniqueNames(NetModel.NetModel model)
        {
            foreach (IGrouping<string, INetType> netTypes in model.NetTypes.GroupBy(c => c.Name))
            {
                if (netTypes.Count() == 1)
                {
                    continue;
                }


                foreach (var netClass in netTypes.OfType<NetClass>().ToList())
                {
                    foreach (var avroField in model.NetTypes.OfType<NetClass>().ToList()
                                 .SelectMany(c => c.Fields)
                                 .Where(f => (f.FieldType == netClass.Name ||
                                              f.FieldType == netClass.Name + "[]" ||
                                              f.FieldType == netClass.Name + "?") &&
                                             f.Namespace == netClass.ClassNamespace))
                    {
                        if (!string.IsNullOrWhiteSpace(avroField.Namespace))
                        {
                            avroField.FieldType = avroField.Namespace + "." + avroField.FieldType;
                        }
                    }

                    //deduplicate classes
                    if (!string.IsNullOrWhiteSpace(netClass.ClassNamespace))
                    {
                        var nameWithNamespace = netClass.ClassNamespace + "." + netClass.Name;
                        if (model.NetTypes.Any(x => x.Name == nameWithNamespace))
                        {
                            model.NetTypes.Remove(netClass);
                        }
                        else
                        {
                            netClass.Name = nameWithNamespace;
                        }
                    }
                }

                //deduplicate enums
                var netEnums = netTypes.OfType<NetEnum>().ToList();
                if (netEnums.Any())
                {
                    foreach (var netEnum in netEnums)
                    {
                        model.NetTypes.Remove(netEnum);
                    }
                    model.NetTypes.Add(netEnums.First());
                }
            }
        }

        internal string ExtractNamespace(JObject typeObj, string longName, string shortName)
        {
            string @namespace = "";
            if (typeObj.ContainsKey("namespace"))
            {
                @namespace = typeObj["namespace"].ToString();
            }
            else
            {
                int place = longName.LastIndexOf(shortName, StringComparison.InvariantCulture);
                if (place >= 0)
                {
                    @namespace = longName.Remove(place, shortName.Length);
                }
            }

            @namespace = @namespace.Replace(".", "");

            return @namespace;
        }
    }
}
