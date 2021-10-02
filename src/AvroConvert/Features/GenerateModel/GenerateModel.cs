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

using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.Features.GenerateModel.Models;
using SolTechnology.Avro.Features.GenerateModel.Resolvers;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.GenerateModel
{
    internal class GenerateModel
    {
        private NamespaceHelper _namespaceHelper;
        private EnumModelResolver _enumResolver;
        private LogicalModelResolver _logicalResolver;
        private ArrayModelResolver _arrayResolver;

        private void Initialize()
        {
            _namespaceHelper = new NamespaceHelper();
            _enumResolver = new EnumModelResolver();
            _logicalResolver = new LogicalModelResolver();
            _arrayResolver = new ArrayModelResolver();
        }


        internal string FromAvroObject(byte[] avroData)
        {
            string schema = AvroConvert.GetSchema(avroData);
            return FromAvroSchema(schema);
        }

        internal string FromAvroSchema(string schema)
        {
            Initialize();

            JObject json = (JObject)JsonConvert.DeserializeObject(schema);

            AvroModel model = new AvroModel();
            Resolve(json, model);
            _namespaceHelper.EnsureUniqueNames(model);

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
                        _enumResolver.ResolveEnum(parent, model);
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
                ClassNamespace = _namespaceHelper.ExtractNamespace(parent, parent["name"].ToString(), shortName)
            };
            model.Classes.Add(c);

            // Get Fields
            foreach (var field in parent["fields"] as JArray)
            {
                if (field is JObject fieldObject)
                {
                    // Get Field type
                    AvroField fieldType = new AvroField();
                    bool isNullable = false;

                    if (field["type"] is JValue)
                    {
                        fieldType = ResolveField(fieldObject);
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
                            fieldObject["type"] = arrayFieldType;
                            fieldType = ResolveField(fieldObject);
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

                    if (isNullable)
                    {
                        fieldType.FieldType += "?";
                    }

                    fieldType.Name = field["name"].ToString();

                    c.Fields.Add(fieldType);
                }
                else
                {
                    throw new InvalidAvroObjectException($"Field type {field.GetType().Name} not supported");
                }
            }
        }

        private AvroField ResolveField(JObject typeObj)
        {
            AvroField result = new AvroField();

            string objectType = typeObj["type"].ToString();

            if (objectType == "record" || objectType == "enum")
            {
                result.FieldType = typeObj["name"].ToString();
            }
            else if (typeObj["type"].ToString() == "array")
            {
                result = _arrayResolver.ResolveArray(typeObj);
            }
            else if (typeObj["logicalType"] != null)
            {
                result.FieldType = _logicalResolver.ResolveLogical(typeObj);
            }
            else
            {
                result.FieldType = objectType;
            }

            string shortType = result.FieldType.Split('.').Last();
            if (string.IsNullOrEmpty(result.Namespace))
                result.Namespace = _namespaceHelper.ExtractNamespace(typeObj, result.FieldType, shortType);

            if (shortType.Contains("boolean"))
                shortType = shortType.Replace("boolean", "bool");
            if (shortType == "bytes")
                shortType = "byte[]";

            result.FieldType = shortType;


            return result;
        }
    }
}
