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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.Features.GenerateModel.NetModel;
using SolTechnology.Avro.Features.GenerateModel.Resolvers;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.Features.GenerateModel
{
    [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
    internal class GenerateModel
    {
        private NamespaceHelper _namespaceHelper;
        private EnumModelResolver _enumResolver;
        private LogicalModelResolver _logicalResolver;
        private Dictionary<string, (int Size, string Name, string Namespace)> _fixed;

        private void Initialize()
        {
            _namespaceHelper = new NamespaceHelper();
            _enumResolver = new EnumModelResolver();
            _logicalResolver = new LogicalModelResolver();
            _fixed = new Dictionary<string, (int, string, string)>();
        }


        internal string FromAvroObject(byte[] avroData)
        {
            string schema = AvroConvert.GetSchema(avroData);
            return FromAvroSchema(schema);
        }

        internal string FromAvroSchema(string schema)
        {
            Initialize();

            JToken json = JToken.Parse(schema, new JsonLoadSettings { CommentHandling = CommentHandling.Ignore });
            if (json is not JObject and not JArray)
            {
                throw new InvalidAvroObjectException($"The schema is {json}: there is no record or enum to generate a class for.");
            }

            NetModel.NetModel model = new NetModel.NetModel();
            CollectFixed(json, null);
            // A file of several schemas (or a union at the root) is a JSON array of them.
            foreach (var root in json is JArray roots ? roots.OfType<JObject>() : new[] { (JObject)json })
            {
                Resolve(root, model);
            }
            _namespaceHelper.EnsureUniqueNames(model);
            MarkEnumDefaults(model);

            StringBuilder sb = new StringBuilder();
            foreach (INetType ac in model.NetTypes)
            {
                ac.Write(sb);
            }

            var usings = Usings(sb.ToString());
            if (usings.Length > 0)
            {
                sb.Insert(0, usings + "\n");
            }

            if (HasNullableReferences(model))
            {
                // string? and Item?[] give ["null", T] only where the compiler keeps the annotation, so the file turns it on
                // whatever the project's <Nullable> is. Annotations only: no warnings about uninitialised members.
                sb.Insert(0, "#nullable enable annotations\n\n");
            }

            // Generated code uses CRLF regardless of the host platform so the output is deterministic.
            return sb.Replace("\r\n", "\n").Replace("\n", "\r\n").ToString();
        }

        private static readonly HashSet<string> ValueTypes = new()
        {
            "bool", "int", "long", "float", "double", "decimal", "DateTime", "DateTimeOffset", "DateOnly", "TimeOnly", "TimeSpan", "Guid"
        };

        /// <summary>
        /// A member type with a nullable reference (string?, Item?, byte[]?, string?[], Dictionary&lt;string,string?&gt;): those
        /// need the annotation context. int? and an enum? are Nullable&lt;T&gt; without it.
        /// </summary>
        private static bool HasNullableReferences(NetModel.NetModel model)
        {
            var valueTypes = new HashSet<string>(ValueTypes.Concat(model.NetTypes.OfType<NetEnum>().Select(e => e.Name)));
            return model.NetTypes.OfType<NetClass>()
                .SelectMany(netClass => netClass.Fields)
                .SelectMany(field => Regex.Matches(field.FieldType, @"([\w\]>]+)\?").Select(match => match.Groups[1].Value))
                .Any(type => !valueTypes.Contains(type));
        }

        /// <summary>The using directives the generated code needs, so that it compiles as it is.</summary>
        private static string Usings(string code)
        {
            var usings = new List<string>();
            if (Regex.IsMatch(code, @"\b(DateTime|DateOnly|TimeOnly|TimeSpan|Guid)\b"))
                usings.Add("System");
            if (code.Contains("Dictionary<"))
                usings.Add("System.Collections.Generic");
            if (code.Contains("[DefaultValue(") || code.Contains("[Description("))
                usings.Add("System.ComponentModel");
            if (code.Contains("[DataContract("))
                usings.Add("System.Runtime.Serialization");
            if (code.Contains("[AvroType(") || code.Contains("[AvroDecimal(") || code.Contains("[AvroUnion(") || code.Contains("[AvroFixed("))
                usings.Add("SolTechnology.Avro.Infrastructure.Attributes");

            return string.Concat(usings.Select(u => $"using {u};\n"));
        }


        private static void MarkEnumDefaults(NetModel.NetModel model)
        {
            var enumNames = model.NetTypes.OfType<NetEnum>().Select(e => e.Name).ToHashSet();
            foreach (var field in model.NetTypes.OfType<NetClass>().SelectMany(c => c.Fields))
            {
                field.DefaultIsEnum = field.Default != null && field.DefaultType != null && enumNames.Contains(field.DefaultType);
            }
        }

        /// <summary>Every named fixed of the schema by full and by short name: a member that refers to one gets [AvroFixed].</summary>
        private void CollectFixed(JToken token, string enclosingNamespace)
        {
            switch (token)
            {
                case JObject obj:
                    var type = obj["type"] as JValue;
                    var ownNamespace = type?.ToString() is "record" or "error" or "enum" or "fixed"
                        ? AvroNamespace(obj, enclosingNamespace)
                        : enclosingNamespace;
                    if (type?.ToString() == "fixed" && obj["name"] != null && obj["size"] != null)
                    {
                        var shortName = obj["name"].ToString().Split('.').Last();
                        var definition = (obj["size"].Value<int>(), shortName, ownNamespace);
                        _fixed[string.IsNullOrEmpty(ownNamespace) ? shortName : ownNamespace + "." + shortName] = definition;
                        _fixed.TryAdd(shortName, definition);
                    }
                    foreach (var property in obj.Properties())
                    {
                        CollectFixed(property.Value, ownNamespace);
                    }
                    break;
                case JArray array:
                    foreach (var item in array)
                    {
                        CollectFixed(item, enclosingNamespace);
                    }
                    break;
            }
        }

        /// <param name="enclosingNamespace">Namespace of the closest named type around <paramref name="json"/>; a named type without its own namespace inherits it.</param>
        private void Resolve(object json, NetModel.NetModel model, string enclosingNamespace = null)
        {
            if (json is JObject parent)
            {
                var type = parent["type"] as JValue;
                var ownNamespace = type?.ToString() is "record" or "error" or "enum" or "fixed"
                    ? AvroNamespace(parent, enclosingNamespace)
                    : enclosingNamespace;

                foreach (var prop in parent)
                {
                    if (prop.Key == "type" && prop.Value.ToString() is "record" or "error")
                    {
                        ResolveRecord(parent, model, ownNamespace);
                    }
                    else if (prop.Key == "type" && prop.Value.ToString() == "enum")
                    {
                        _enumResolver.ResolveEnum(parent, model, ownNamespace);
                    }
                    else if (prop.Value is JObject)
                    {
                        Resolve(prop.Value, model, ownNamespace);
                    }
                    else if (prop.Value is JArray array)
                    {
                        foreach (var arrayItem in array)
                        {
                            switch (arrayItem)
                            {
                                case JObject jObject:
                                    Resolve(jObject, model, ownNamespace);
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

        /// <summary>Namespace of a named type as the Avro specification defines it: from a dotted name, the "namespace" attribute, or the enclosing type.</summary>
        internal static string AvroNamespace(JObject named, string enclosingNamespace)
        {
            var name = named["name"]?.ToString() ?? "";
            var dot = name.LastIndexOf('.');
            if (dot >= 0)
            {
                return name.Substring(0, dot);
            }

            return named["namespace"]?.ToString() ?? enclosingNamespace;
        }

        private void ResolveRecord(JObject parent, NetModel.NetModel model, string avroNamespace)
        {
            var shortName = parent["name"].ToString().Split('.').Last();
            NetClass c = new NetClass()
            {
                Name = shortName,
                // The namespace the record has, also when it inherits it: two records of one name in two namespaces get apart.
                ClassNamespace = (avroNamespace ?? "").Replace(".", ""),
                AvroName = shortName,
                AvroNamespace = avroNamespace,
                Doc = parent["doc"] is JValue { Type: JTokenType.String } doc ? doc.ToString() : null
            };
            model.NetTypes.Add(c);

            // Get Fields
            foreach (var field in parent["fields"] as JArray)
            {
                if (field is JObject fieldObject)
                {
                    // A type given by name can have its definition on the field itself ({"name": "m", "type": "map", "values": "int"}).
                    // (Not valid Avro, both parsers reject it, but older versions generated a model for it.)
                    NetClassField fieldType = field["type"] is JValue
                        ? ResolveObject(fieldObject, avroNamespace, onField: true)
                        : ResolveType(field["type"], avroNamespace);

                    fieldType.Name = field["name"].ToString();

                    if (field["default"] is JValue { Type: not JTokenType.Null } _value)
                    {
                        fieldType.Default = _value.ToString(NumberFormatInfo.InvariantInfo);
                        fieldType.DefaultIsString = _value.Type == JTokenType.String;
                    }

                    if (field["doc"] is JValue { Type: JTokenType.String } fieldDoc)
                    {
                        fieldType.Doc = fieldDoc.ToString();
                    }

                    c.Fields.Add(fieldType);
                }
                else
                {
                    throw new InvalidAvroObjectException($"Field type {field.GetType().Name} not supported");
                }
            }
        }

        private static readonly Dictionary<string, string> Primitives = new()
        {
            ["null"] = "object", ["boolean"] = "bool", ["int"] = "int", ["long"] = "long", ["float"] = "float",
            ["double"] = "double", ["bytes"] = "byte[]", ["string"] = "string"
        };

        /// <summary>
        /// The .NET type of an Avro type, with the attributes that keep its schema. Recursive: arrays, maps and unions take the
        /// type of what they hold, so an array of arrays, a map of records defined in place or a union of any branches resolve.
        /// </summary>
        private NetClassField ResolveType(JToken type, string enclosingNamespace)
        {
            switch (type)
            {
                case JValue value:
                    return ResolveName(value.ToString(), enclosingNamespace);
                case JArray union:
                    return ResolveUnion(union, enclosingNamespace);
                case JObject obj:
                    return ResolveObject(obj, enclosingNamespace);
                default:
                    throw new InvalidAvroObjectException($"Unable to process field type of {type?.GetType().Name}");
            }
        }

        private NetClassField ResolveName(string name, string enclosingNamespace)
        {
            if (Primitives.TryGetValue(name, out var primitive))
            {
                return new NetClassField { FieldType = primitive };
            }

            if (name == "fixed")
            {
                // "type": "fixed" without a definition of its own.
                return new NetClassField { FieldType = "byte[]" };
            }

            if (FindFixed(name, enclosingNamespace) is { } definition)
            {
                return Fixed(definition);
            }

            // A record or an enum referred to by name.
            return Named(new JObject { ["type"] = name }, name, enclosingNamespace);
        }

        private NetClassField ResolveObject(JObject obj, string enclosingNamespace, bool onField = false)
        {
            switch (obj["type"])
            {
                case JObject definition:
                    {
                        // Apache.Avro for C# writes a logical type on a named type around its definition:
                        // {"type": {"type": "fixed", ...}, "logicalType": "decimal"}. It is the definition with the outer attributes.
                        var merged = (JObject)definition.DeepClone();
                        foreach (var property in obj.Properties().Where(p => p.Name != "type"))
                        {
                            merged[property.Name] = property.Value.DeepClone();
                        }
                        return ResolveObject(merged, enclosingNamespace);
                    }
                case JArray union:
                    return ResolveUnion(union, enclosingNamespace);
            }

            string objectType = obj["type"].ToString();
            if (obj["logicalType"] != null)
            {
                var (logicalType, attribute) = _logicalResolver.ResolveLogical(obj);
                if (logicalType != null)
                {
                    var logical = new NetClassField { FieldType = logicalType };
                    if (attribute != null)
                    {
                        logical.Attributes.Add(attribute);
                    }
                    return logical;
                }
            }

            switch (objectType)
            {
                case "record":
                case "error":
                case "enum":
                    return Named(obj, obj["name"].ToString(), enclosingNamespace);
                case "fixed":
                    // On a field, "name" is the name of the field, not of a fixed.
                    return !onField && obj["name"] != null && obj["size"] != null
                        ? Fixed((obj["size"].Value<int>(), obj["name"].ToString().Split('.').Last(), AvroNamespace(obj, enclosingNamespace)))
                        : new NetClassField { FieldType = "byte[]" };
                case "array":
                    {
                        var items = ResolveType(obj["items"], enclosingNamespace);
                        var array = new NetClassField { FieldType = items.FieldType + "[]", Namespace = items.Namespace };
                        // The representation of the items ([AvroType], [AvroDecimal]) is given on the member; [AvroFixed] is not.
                        array.Attributes.AddRange(items.Attributes.Where(a => !a.StartsWith("[AvroFixed(")));
                        return array;
                    }
                case "map":
                    {
                        var values = ResolveType(obj["values"], enclosingNamespace);
                        var map = new NetClassField { FieldType = $"Dictionary<string,{values.FieldType}>" };
                        map.Attributes.AddRange(values.Attributes.Where(a => !a.StartsWith("[AvroFixed(")));
                        return map;
                    }
                default:
                    // A primitive written as an object ({"type": "string"}), possibly with an unknown logical type.
                    return ResolveName(objectType, enclosingNamespace);
            }
        }

        /// <summary>
        /// [null, T] and [T, null] are a nullable T. Other unions are an object with every branch in [AvroUnion], so the class
        /// gives back the same branches. A representation a branch needs ([AvroType] of a timestamp) is put on the member.
        /// </summary>
        private NetClassField ResolveUnion(JArray union, string enclosingNamespace)
        {
            var branches = union.Where(t => t.ToString() != "null").ToList();
            bool nullable = branches.Count < union.Count;

            if (branches.Count == 1)
            {
                var single = ResolveType(branches[0], enclosingNamespace);
                if (nullable)
                {
                    single.FieldType += "?";
                }
                return single;
            }

            var result = new NetClassField { FieldType = nullable ? "object?" : "object" };
            foreach (var branch in branches)
            {
                var member = ResolveType(branch, enclosingNamespace);
                result.UnionMembers.Add(member);
                foreach (var attribute in member.Attributes.Where(a => !result.Attributes.Contains(a)))
                {
                    result.Attributes.Add(attribute);
                }
            }

            // [AvroFixed] names the byte[] branch; with two fixed branches (or a fixed and bytes) the byte[] would be ambiguous.
            if (result.Attributes.Count(a => a.StartsWith("[AvroFixed(")) > 1 || result.UnionMembers.Count(m => m.FieldType == "byte[]") > 1)
            {
                result.Attributes.RemoveAll(a => a.StartsWith("[AvroFixed("));
            }
            return result;
        }

        private (int Size, string Name, string Namespace)? FindFixed(string name, string enclosingNamespace)
        {
            if (!name.Contains('.') && !string.IsNullOrEmpty(enclosingNamespace) && _fixed.TryGetValue(enclosingNamespace + "." + name, out var inNamespace))
            {
                return inNamespace;
            }
            return _fixed.TryGetValue(name, out var definition) ? definition : null;
        }

        private static NetClassField Fixed((int Size, string Name, string Namespace) definition)
        {
            var field = new NetClassField { FieldType = "byte[]" };
            field.Attributes.Add(string.IsNullOrEmpty(definition.Namespace)
                ? $"[AvroFixed({definition.Size}, \"{definition.Name}\")]"
                : $"[AvroFixed({definition.Size}, \"{definition.Name}\", \"{definition.Namespace}\")]");
            return field;
        }

        /// <summary>A record or an enum: its short name, and the namespace that tells two of the same name apart.</summary>
        private static NetClassField Named(JObject typeObj, string name, string enclosingNamespace)
        {
            string shortType = name.Split('.').Last();
            // A reference by short name is to the type in the enclosing namespace, as a definition without a namespace of its own.
            var avroNamespace = name.Contains('.') || typeObj["namespace"] != null ? AvroNamespace(typeObj.ContainsKey("name") ? typeObj : new JObject { ["name"] = name }, enclosingNamespace) : enclosingNamespace;
            return new NetClassField
            {
                FieldType = shortType,
                Namespace = (avroNamespace ?? "").Replace(".", "")
            };
        }
    }
}
