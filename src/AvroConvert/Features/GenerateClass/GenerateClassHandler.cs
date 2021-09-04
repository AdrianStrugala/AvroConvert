using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Schema.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.GenerateClass
{
    public class GenerateClassHandler
    {
        public string HandleAvroObject(byte[] avroData)
        {
            string schema = AvroConvert.GetSchema(avroData);
            return HandleAvroSchema(schema);
        }

        public string HandleAvroSchema(string schema)
        {
            JObject json = (JObject)JsonConvert.DeserializeObject(schema);

            List<AvroClass> classes = new List<AvroClass>();
            GetRecordClasses(json, classes);

            StringBuilder sb = new StringBuilder();
            foreach (AvroClass ac in classes)
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

            return sb.ToString();
        }

        private void GetRecordClasses(object json, List<AvroClass> classes)
        {
            if (json is JObject parent)
            {
                foreach (var prop in parent)
                {
                    if (prop.Key == "type" && prop.Value.ToString() == "record")
                    {
                        AvroClass c = new AvroClass()
                        {
                            ClassName = parent["name"].ToString().Split('.').Last()
                        };
                        classes.Add(c);

                        // Get Fields
                        foreach (var field in parent["fields"] as JArray)
                        {
                            if (field is JObject)
                            {
                                // Get Field type
                                string fieldType;
                                bool isNullable = false;

                                if (field["type"] is JValue)
                                {
                                    fieldType = field["type"].ToString();
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
                                        throw new InvalidAvroObjectException($"Unable to determine acceptable data type for {field["type"]}");

                                    // Is the field type an object that's defined in this spot
                                    JToken arrayFieldType = types.FirstOrDefault(x => x.ToString() != "null");
                                    if (arrayFieldType is JValue)
                                    {
                                        fieldType = arrayFieldType.ToString();
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

                                if (fieldType.Contains("boolean"))
                                    fieldType = fieldType.Replace("boolean", "bool");
                                if (fieldType == "bytes")
                                    fieldType = "byte[]";

                                if (isNullable)
                                {
                                    fieldType += "?";
                                }

                                c.Fields.Add(new AvroField()
                                {
                                    Name = field["name"].ToString(),
                                    FieldType = fieldType.Split('.').Last()
                                });
                            }
                            else
                            {
                                throw new InvalidAvroObjectException($"Field type {field.GetType().Name} not supported");
                            }
                        }
                    }
                    if (prop.Value is JObject)
                    {
                        GetRecordClasses(prop.Value, classes);
                    }
                    else if (prop.Value is JArray array)
                    {
                        foreach (var arrayItem in array)
                        {
                            if (arrayItem is JObject jObject)
                            {
                                GetRecordClasses(jObject, classes);
                            }
                            else if (arrayItem is JValue)
                            {
                                // This could be any item in an array
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

        private string ResolveField(JObject typeObj)
        {
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

            return fieldType;
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


        public class AvroClass
        {
            public string ClassName { get; set; }
            public List<AvroField> Fields { get; set; } = new List<AvroField>();
        }

        public class AvroField
        {
            public string FieldType { get; set; }
            public string Name { get; set; }
        }
    }
}
