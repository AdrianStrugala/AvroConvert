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

using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.Features.GenerateModel.NetModel;
using SolTechnology.Avro.Features.GenerateModel.Resolvers;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.Features.GenerateModel
{
    internal class GenerateModel
    {
        private NamespaceHelper _namespaceHelper;
        private EnumModelResolver _enumResolver;
        private LogicalModelResolver _logicalResolver;
        private ArrayModelResolver _arrayResolver;
        private MapModelResolver _mapModelResolver;

        private void Initialize()
        {
            _namespaceHelper = new NamespaceHelper();
            _enumResolver = new EnumModelResolver();
            _logicalResolver = new LogicalModelResolver();
            _arrayResolver = new ArrayModelResolver();
            _mapModelResolver = new MapModelResolver();
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

            NetModel.NetModel model = new NetModel.NetModel();
            Resolve(json, model);
            _namespaceHelper.EnsureUniqueNames(model);

            StringBuilder sb = new StringBuilder();
            foreach (INetType ac in model.NetTypes)
            {
                ac.Write(sb);
            }

            return sb.ToString();
        }


        private void Resolve(object json, NetModel.NetModel model)
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
                            switch (arrayItem)
                            {
                                case JObject jObject:
                                    Resolve(jObject, model);
                                    break;
                                case JValue _:
                                    // This could be any item in an array - for example nullable
                                    break;
                                default:
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

        private void ResolveRecord(JObject parent, NetModel.NetModel model)
        {
            var shortName = parent["name"].ToString().Split('.').Last();
            NetClass c = new NetClass()
            {
                Name = shortName,
                ClassNamespace = _namespaceHelper.ExtractNamespace(parent, parent["name"].ToString(), shortName)
            };
            model.NetTypes.Add(c);

            // Get Fields
            foreach (var field in parent["fields"] as JArray)
            {
                if (field is JObject fieldObject)
                {
                    // Get Field type
                    NetClassField fieldType = new NetClassField();
                    bool isNullable = false;

                    switch (field["type"])
                    {
                        case JValue _:
                            fieldType = ResolveField(fieldObject);
                            break;
                        case JObject fieldJObject:
                            fieldType = ResolveField(fieldJObject);
                            break;
                        case JArray types:
                            {
                                if (types.Any(t => t.ToString() == "null"))
                                {
                                    isNullable = true;
                                }

                                if (types.Count > 2)
                                    throw new InvalidAvroObjectException(
                                        $"Unable to determine acceptable data type for {field["type"]}");

                                // Is the field type an object that's defined in this spot
                                JToken arrayFieldType = types.FirstOrDefault(x => x.ToString() != "null");

                                switch (arrayFieldType)
                                {
                                    case JValue _:
                                        fieldObject["type"] = arrayFieldType;
                                        fieldType = ResolveField(fieldObject);
                                        break;
                                    case JObject arrayFieldJObject:
                                        fieldType = ResolveField(arrayFieldJObject);
                                        break;
                                    default:
                                        throw new InvalidAvroObjectException($"Unable to create array in array {arrayFieldType}");
                                }

                                break;
                            }
                        default:
                            throw new InvalidAvroObjectException($"Unable to process field type of {field["type"].GetType().Name}");
                    }

                    if (isNullable)
                    {
                        fieldType.FieldType += "?";
                    }

                    fieldType.Name = field["name"].ToString();

                    if (field["default"] is JValue _value)
                    {
                        fieldType.Default = _value.ToString(NumberFormatInfo.InvariantInfo);                    
                    }

                    if (field["doc"] is JValue)
                    {
                        fieldType.Doc = field["doc"].ToString();
                    }

                    c.Fields.Add(fieldType);
                }
                else
                {
                    throw new InvalidAvroObjectException($"Field type {field.GetType().Name} not supported");
                }
            }
        }

        private NetClassField ResolveField(JObject typeObj)
        {
            NetClassField result = new NetClassField();

            string objectType = typeObj["type"].ToString();

            switch (objectType)
            {
                case "record":
                case "enum":
                    result.FieldType = typeObj["name"].ToString();
                    break;
                case "array":
                    result = _arrayResolver.ResolveArray(typeObj);
                    break;
                case "map":
                    result.FieldType = _mapModelResolver.ResolveMap(typeObj);
                    break;
                default:
                    {
                        if (typeObj["logicalType"] != null)
                        {
                            result.FieldType = _logicalResolver.ResolveLogical(typeObj);
                        }
                        else
                        {
                            result.FieldType = objectType;
                        }

                        break;
                    }
            }

            string shortType = result.FieldType.Split('.').Last();
            if (string.IsNullOrEmpty(result.Namespace))
                result.Namespace = _namespaceHelper.ExtractNamespace(typeObj, result.FieldType, shortType);

            shortType = shortType.TryReplace("boolean", "bool");
            shortType = shortType.TryReplace("bytes", "byte[]");
            shortType = shortType.TryReplace("fixed", "byte[]");
            
            result.FieldType = shortType;


            return result;
        }
        
    }
}
