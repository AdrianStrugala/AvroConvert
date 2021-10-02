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

namespace SolTechnology.Avro.Features.GenerateModel
{
    internal class NamespaceHelper
    {
        internal void EnsureUniqueNames(AvroModel model)
        {
            foreach (IGrouping<string, AvroClass> avroClasses in model.Classes.GroupBy(c => c.ClassName))
            {
                if (avroClasses.Count() == 1)
                {
                    continue;
                }

                foreach (var avroClass in avroClasses)
                {
                    foreach (var avroField in model.Classes
                        .SelectMany(c => c.Fields)
                        .Where(f => (f.FieldType == avroClass.ClassName ||
                                    f.FieldType == avroClass.ClassName + "[]" ||
                                    f.FieldType == avroClass.ClassName + "?") &&
                                    f.Namespace == avroClass.ClassNamespace))
                    {
                        avroField.FieldType = avroField.Namespace + avroField.FieldType;
                    }

                    avroClass.ClassName = avroClass.ClassNamespace + avroClass.ClassName;
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
