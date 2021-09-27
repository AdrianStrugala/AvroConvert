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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Schema.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.GenerateModel
{
    internal class GenerateModel
    {
        internal string FromAvroObject(byte[] avroData)
        {
            string schema = AvroConvert.GetSchema(avroData);
            return FromAvroSchema(schema);
        }

        internal string FromAvroSchema(string schema)
        {
            JObject json = (JObject)JsonConvert.DeserializeObject(schema);

            AvroModel model = new AvroModel();
            Resolve(json, model);
            EnsureUniqueNames(model);

            StringBuilder sb = new StringBuilder();
            foreach (AvroClass ac in model.Classes)
            {
                sb.AppendLine($"public class {ac.ClassName}");
                sb.AppendLine("{");
                foreach (AvroField field in ac.Fields)
                {
                    sb.AppendLine($"	public {field.FieldType} {field.Name} {{ get; set; }}");
                }
                sb.AppendLine("}");
                sb.AppendLine();
            }

            foreach (AvroEnum @enum in model.Enums)
            {
                sb.AppendLine($"public enum {@enum.EnumName}");
                sb.AppendLine("{");
                foreach (string symbol in @enum.Symbols)
                {
                    sb.AppendLine($"	{symbol}");
                }
                sb.AppendLine("}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private void EnsureUniqueNames(AvroModel model)
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
                                                    .Where(f => f.FieldType == avroClass.ClassName && f.Namespace == avroClass.ClassNamespace))
                    {
                        avroField.FieldType = avroField.Namespace + avroField.FieldType;
                    }

                    avroClass.ClassName = avroClass.ClassNamespace + avroClass.ClassName;
                }
            }
        }

        private void Resolve(object json, AvroModel model)
        {
            if (json is JObject parent)
            {
                foreach (var prop in parent)
                {
                    if (prop.Key == "type" && prop.Value.ToString() == "record")
                    {
                        ResolveRecord(parent, model);
                    }
                    else if (prop.Key == "type" && prop.Value.ToString() == "enum")
                    {
                        ResolveEnum(parent, model);
                    }
                    else if (prop.Value is JObject)
                    {
                        Resolve(prop.Value, model);
                    }
                    else if (prop.Value is JArray array)
                    {
                        foreach (var arrayItem in array)
                        {
                            if (arrayItem is JObject jObject)
                            {
                                Resolve(jObject, model);
                            }
                            else if (arrayItem is JValue)
                            {
                                // This could be any item in an array - for example nullable
                            }
                            else
                            {
                                throw new InvalidAvroObjectException($"Unhandled newtonsoft type {arrayItem.GetType().Name}");
                            }
                        }
                    }
                }
            }
            else if (json is JArray)
            {
                throw new InvalidAvroObjectException($"Unhandled array on root level");
            }
            else
            {
                throw new InvalidAvroObjectException($"Unidentified newtonsoft object type {json.GetType().Name}");
            }
        }

        private void ResolveRecord(JObject parent, AvroModel model)
        {
            var shortName = parent["name"].ToString().Split('.').Last();
            AvroClass c = new AvroClass()
            {
                ClassName = shortName,
                ClassNamespace = ExtractNamespace(parent, parent["name"].ToString(), shortName)
            };
            model.Classes.Add(c);

            // Get Fields
            foreach (var field in parent["fields"] as JArray)
            {
                if (field is JObject)
                {
                    // Get Field type
                    AvroField fieldType = new AvroField();
                    bool isNullable = false;

                    if (field["type"] is JValue)
                    {
                        fieldType.FieldType = field["type"].ToString();
                    }
                    else if (field["type"] is JObject fieldJObject)
                    {
                        fieldType = ResolveField(fieldJObject);
                    }
                    else if (field["type"] is JArray types)
                    {
                        foreach (var e in types)
                        {
                            if (e.ToString() == "null")
                                isNullable = true;
                        }

                        if (types.Count > 2)
                            throw new InvalidAvroObjectException(
                                $"Unable to determine acceptable data type for {field["type"]}");

                        // Is the field type an object that's defined in this spot
                        JToken arrayFieldType = types.FirstOrDefault(x => x.ToString() != "null");
                        if (arrayFieldType is JValue)
                        {
                            fieldType.FieldType = arrayFieldType.ToString();
                        }
                        else if (arrayFieldType is JObject arrayFieldJObject)
                        {
                            fieldType = ResolveField(arrayFieldJObject);
                        }
                        else
                        {
                            throw new InvalidAvroObjectException($"Unable to create array in array {arrayFieldType}");
                        }
                    }
                    else
                    {
                        throw new InvalidAvroObjectException($"Unable to process field type of {field["type"].GetType().Name}");
                    }

                    if (fieldType.FieldType.Contains("boolean"))
                        fieldType.FieldType = fieldType.FieldType.Replace("boolean", "bool");
                    if (fieldType.FieldType == "bytes")
                        fieldType.FieldType = "byte[]";

                    fieldType.Name = field["name"].ToString();
                    fieldType.FieldType = fieldType.FieldType.Split('.').Last();

                    if (isNullable)
                    {
                        fieldType.FieldType += "?";
                    }

                    c.Fields.Add(fieldType);
                }
                else
                {
                    throw new InvalidAvroObjectException($"Field type {field.GetType().Name} not supported");
                }
            }
        }

        // private AvroField BuildAvroField(JToken field, string fieldType)
        // {
        //     string shortType = fieldType = fieldType.Split('.').Last();
        //     string @namespace;
        //
        //  
        //
        //     return new AvroField
        //     {
        //         Name = field["name"].ToString(),
        //         Namespace = @namespace,
        //         FieldType = shortType
        //     };
        // }

        private void ResolveEnum(JToken propValue, AvroModel model)
        {
            var result = new AvroEnum();

            var name = propValue["name"].ToString().Split('.').Last();
            var symbols = (JArray)propValue["symbols"];

            result.EnumName = name;
            result.Symbols = symbols.Select(s => s.ToString()).ToList();

            model.Enums.Add(result);
        }

        private AvroField ResolveField(JObject typeObj)
        {
            AvroField result = new AvroField();
            string fieldType;

            string objectType = typeObj["type"].ToString();
            if (objectType == "record" || objectType == "enum")
            {
                fieldType = typeObj["name"].ToString();
            }
            else if (typeObj["type"].ToString() == "array")
            {
                fieldType = ResolveArray(typeObj);
            }
            else if (typeObj["logicalType"] != null)
            {
                fieldType = ResolveLogical(typeObj);
            }
            else
            {
                throw new InvalidAvroObjectException($"Unable to process field type of {typeObj["type"]}");
            }

            string shortType = fieldType.Split('.').Last();

            result.FieldType = shortType;
            result.Namespace = ExtractNamespace(typeObj, fieldType, shortType);

            return result;
        }

        private static string ExtractNamespace(JObject typeObj, string longName, string shortName)
        {
            string @namespace;
            if (typeObj.ContainsKey("namespace"))
            {
                @namespace = typeObj["namespace"].ToString();
            }
            else
            {
                int place = longName.LastIndexOf(shortName, StringComparison.InvariantCulture);
                @namespace = longName.Remove(place, shortName.Length);
            }

            @namespace = @namespace.Replace(".", "");

            return @namespace;
        }

        private string ResolveLogical(JObject typeObj)
        {
            string logicalType = typeObj["logicalType"].ToString();

            switch (logicalType)
            {
                case LogicalTypeSchema.LogicalTypeEnum.Date:
                case LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds:
                case LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds:
                    return "DateTime";
                case LogicalTypeSchema.LogicalTypeEnum.Decimal:
                    return "decimal";
                case LogicalTypeSchema.LogicalTypeEnum.Duration:
                case LogicalTypeSchema.LogicalTypeEnum.TimeMicrosecond:
                case LogicalTypeSchema.LogicalTypeEnum.TimeMilliseconds:
                    return "TimeSpan";
                case LogicalTypeSchema.LogicalTypeEnum.Uuid:
                    return "Guid";
                default:
                    throw new InvalidAvroObjectException($"Unidentified logicalType {logicalType}");
            }
        }

        private static string ResolveArray(JObject typeObj)
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

        internal class AvroModel
        {
            internal List<AvroClass> Classes { get; set; } = new List<AvroClass>();
            internal List<AvroEnum> Enums { get; set; } = new List<AvroEnum>();
        }

        internal class AvroEnum
        {
            public string EnumName { get; set; }
            public List<string> Symbols { get; set; } = new List<string>();
        }

        internal class AvroClass
        {
            public string ClassName { get; set; }
            public string ClassNamespace { get; set; }
            public List<AvroField> Fields { get; set; } = new List<AvroField>();
        }

        internal class AvroField
        {
            public string FieldType { get; set; }
            public string Name { get; set; }
            public string Namespace { get; set; }
        }
    }
}
