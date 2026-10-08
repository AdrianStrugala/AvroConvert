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
                            avroField.FieldType = avroField.Namespace + avroField.FieldType;
                        }
                    }

                    //deduplicate classes
                    if (!string.IsNullOrWhiteSpace(netClass.ClassNamespace))
                    {
                        var nameWithNamespace = netClass.ClassNamespace + netClass.Name;
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
