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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.AvroObjectServices.Write.Typed;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.Features.JsonEncoding
{
    /// <summary>
    /// JSON → Avro binary as defined in the "JSON Encoding" section of the Avro specification. The input holds one or more
    /// JSON values (separated by whitespace or new lines); each becomes one entry of the object container file.
    /// </summary>
    internal sealed class JsonEncodingReader
    {
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal byte[] Convert(string json, TypeSchema schema, CodecType codecType)
        {
            var values = ReadValues(json);

            using var output = new MemoryStream();
            using (var encoder = new Encoder(schema, output, codecType))
            {
                foreach (var value in values)
                {
                    encoder.AppendEncoded(writer => Write(schema, value, writer, "$"));
                }
            }

            return output.ToArray();
        }

        private static List<JToken> ReadValues(string json)
        {
            var values = new List<JToken>();
            using var reader = new JsonTextReader(new StringReader(json))
            {
                SupportMultipleContent = true,
                // Strings must stay strings (no DateTime guessing) and numbers keep full precision.
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Double
            };

            while (reader.Read())
            {
                values.Add(JToken.ReadFrom(reader));
            }

            return values;
        }

        /// <summary>
        /// Binary encoding of a schema <c>default</c>. Defaults use the JSON encoding except for unions, whose default is the
        /// unwrapped value of a branch (the first one that accepts it).
        /// </summary>
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal static byte[] EncodeDefault(TypeSchema schema, JToken defaultJson)
        {
            using var stream = new MemoryStream();
            Write(schema, defaultJson, new Writer(stream), "default", isDefault: true);
            return stream.ToArray();
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal static void Write(TypeSchema schema, JToken token, IWriter writer, string path, bool isDefault = false)
        {
            switch (schema)
            {
                case LogicalTypeSchema logical:
                    Write(logical.BaseTypeSchema, token, writer, path, isDefault);
                    return;
                case NullableSchema nullable:
                    Write(nullable.ValueSchema, token, writer, path, isDefault);
                    return;
            }

            switch (schema.Type)
            {
                case AvroType.Null:
                    Expect(token, JTokenType.Null, schema, path);
                    writer.WriteNull();
                    break;
                case AvroType.Boolean:
                    Expect(token, JTokenType.Boolean, schema, path);
                    writer.WriteBoolean(token.Value<bool>());
                    break;
                case AvroType.Int:
                    Expect(token, JTokenType.Integer, schema, path);
                    writer.WriteInt(Integer<int>(token, schema, path));
                    break;
                case AvroType.Long:
                    Expect(token, JTokenType.Integer, schema, path);
                    writer.WriteLong(Integer<long>(token, schema, path));
                    break;
                case AvroType.Float:
                    writer.WriteFloat((float)FloatingPoint(token, schema, path));
                    break;
                case AvroType.Double:
                    writer.WriteDouble(FloatingPoint(token, schema, path));
                    break;
                case AvroType.Bytes:
                    writer.WriteBytes(Bytes(token, schema, path));
                    break;
                case AvroType.String:
                    Expect(token, JTokenType.String, schema, path);
                    writer.WriteString(token.Value<string>());
                    break;
                case AvroType.Record:
                case AvroType.Error:
                    WriteRecord((RecordSchema)schema, token, writer, path, isDefault);
                    break;
                case AvroType.Enum:
                    Expect(token, JTokenType.String, schema, path);
                    var enumSchema = (EnumSchema)schema;
                    var symbol = token.Value<string>();
                    var position = enumSchema.GetSymbolPosition(symbol);
                    if (position < 0)
                    {
                        throw Mismatch(path, $"'{symbol}' is not a symbol of enum {enumSchema.FullName}");
                    }
                    writer.WriteEnum(position);
                    break;
                case AvroType.Fixed:
                    var fixedSchema = (FixedSchema)schema;
                    var bytes = Bytes(token, schema, path);
                    if (bytes.Length != fixedSchema.Size)
                    {
                        throw Mismatch(path, $"fixed {fixedSchema.FullName} requires {fixedSchema.Size} bytes but the string holds {bytes.Length}");
                    }
                    writer.WriteFixed(bytes);
                    break;
                case AvroType.Array:
                    Expect(token, JTokenType.Array, schema, path);
                    var items = (JArray)token;
                    var itemSchema = ((ArraySchema)schema).ItemSchema;
                    writer.WriteArrayStart();
                    writer.WriteItemCount(items.Count);
                    for (int i = 0; i < items.Count; i++)
                    {
                        Write(itemSchema, items[i], writer, $"{path}[{i}]", isDefault);
                    }
                    writer.WriteArrayEnd();
                    break;
                case AvroType.Map:
                    Expect(token, JTokenType.Object, schema, path);
                    var entries = ((JObject)token).Properties().ToList();
                    var valueSchema = ((MapSchema)schema).ValueSchema;
                    writer.WriteMapStart();
                    writer.WriteItemCount(entries.Count);
                    foreach (var entry in entries)
                    {
                        writer.WriteString(entry.Name);
                        Write(valueSchema, entry.Value, writer, $"{path}.{entry.Name}", isDefault);
                    }
                    writer.WriteMapEnd();
                    break;
                case AvroType.Union when isDefault:
                    WriteUnionDefault((UnionSchema)schema, token, writer, path);
                    break;
                case AvroType.Union:
                    WriteUnion((UnionSchema)schema, token, writer, path);
                    break;
                default:
                    throw new AvroException($"Unknown schema type: {schema.Type}");
            }
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static void WriteRecord(RecordSchema schema, JToken token, IWriter writer, string path, bool isDefault)
        {
            Expect(token, JTokenType.Object, schema, path);
            var obj = (JObject)token;

            foreach (var field in schema.Fields)
            {
                var fieldPath = $"{path}.{field.Name}";
                if (obj.TryGetValue(field.Name, StringComparison.Ordinal, out var value))
                {
                    Write(field.TypeSchema, value, writer, fieldPath, isDefault);
                }
                else if (field.DefaultJson != null)
                {
                    Write(field.TypeSchema, field.DefaultJson, writer, fieldPath, isDefault: true);
                }
                else if (field.HasDefaultValue)
                {
                    // Default declared on a .NET member ([DefaultValue]) – only its CLR value is known.
                    WriteDefault(field, writer);
                }
                else
                {
                    throw Mismatch(fieldPath, $"field '{field.Name}' of record {schema.FullName} is missing and has no default");
                }
            }

            // Properties that are not fields of the record are ignored, as by the Java JsonDecoder.
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static void WriteDefault(RecordFieldSchema field, IWriter writer)
        {
            if (field.DefaultValue == null)
            {
                WriteHelpers.WriteNullValue(field.TypeSchema, writer);
                return;
            }

            WritePlanCache.Get(field.TypeSchema, field.DefaultValue.GetType(), null)(field.DefaultValue, writer);
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static void WriteUnion(UnionSchema schema, JToken token, IWriter writer, string path)
        {
            var branches = schema.Schemas;

            if (token.Type == JTokenType.Null)
            {
                for (int i = 0; i < branches.Count; i++)
                {
                    if (JsonEncodingRules.Unwrap(branches[i]).Type == AvroType.Null)
                    {
                        writer.WriteUnionIndex(i);
                        writer.WriteNull();
                        return;
                    }
                }

                throw Mismatch(path, $"null is not a branch of union {schema}");
            }

            if (token is not JObject { Count: 1 } wrapper)
            {
                throw Mismatch(path, $"a non-null union value must be an object with a single property naming the branch, e.g. {{\"{JsonEncodingRules.BranchName(branches.First(b => JsonEncodingRules.Unwrap(b).Type != AvroType.Null))}\": ...}}, but found {token.Type}");
            }

            var property = wrapper.Properties().Single();
            for (int i = 0; i < branches.Count; i++)
            {
                var branch = branches[i];
                if (BranchMatches(branch, property.Name))
                {
                    writer.WriteUnionIndex(i);
                    Write(branch, property.Value, writer, $"{path}.{property.Name}");
                    return;
                }
            }

            throw Mismatch(path, $"'{property.Name}' is not a branch of union {schema}; expected one of {string.Join(", ", branches.Select(JsonEncodingRules.BranchName))}");
        }

        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        private static void WriteUnionDefault(UnionSchema schema, JToken token, IWriter writer, string path)
        {
            // Avro 1.12: the default matches one of the branches, the first one traditionally; try them in order.
            for (int i = 0; i < schema.Schemas.Count; i++)
            {
                using var attempt = new MemoryStream();
                try
                {
                    Write(schema.Schemas[i], token, new Writer(attempt), path, isDefault: true);
                }
                catch (AvroTypeMismatchException)
                {
                    continue;
                }

                writer.WriteUnionIndex(i);
                writer.WriteBytesRaw(attempt.ToArray());
                return;
            }

            throw Mismatch(path, $"default {token.ToString(Formatting.None)} matches no branch of union {schema}");
        }

        private static bool BranchMatches(TypeSchema branch, string name)
        {
            var branchName = JsonEncodingRules.BranchName(branch);
            if (branchName == name)
            {
                return true;
            }

            // Lenient: a named type may also be referred to by its short name.
            return JsonEncodingRules.Unwrap(branch) is NamedSchema named && named.Name == name;
        }

        private static T Integer<T>(JToken token, TypeSchema schema, string path) where T : struct
        {
            try
            {
                return token.Value<T>();
            }
            catch (OverflowException)
            {
                throw Mismatch(path, $"{token} is out of range of {schema.Type.ToString().ToLowerInvariant()}");
            }
        }

        private static double FloatingPoint(JToken token, TypeSchema schema, string path)
        {
            switch (token.Type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                    return token.Value<double>();
                case JTokenType.String:
                    var text = token.Value<string>();
                    switch (text)
                    {
                        case "NaN":
                            return double.NaN;
                        case "Infinity":
                            return double.PositiveInfinity;
                        case "-Infinity":
                            return double.NegativeInfinity;
                    }
                    break;
            }

            throw Mismatch(path, $"expected a number for {schema.Type.ToString().ToLowerInvariant()} but found {token.Type}");
        }

        private static byte[] Bytes(JToken token, TypeSchema schema, string path)
        {
            Expect(token, JTokenType.String, schema, path);
            var text = token.Value<string>();
            var result = new byte[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] > 0xFF)
                {
                    throw Mismatch(path, $"character U+{(int)text[i]:X4} at position {i} is not a byte; bytes and fixed are strings of code points 0-255");
                }
                result[i] = (byte)text[i];
            }
            return result;
        }

        private static void Expect(JToken token, JTokenType expected, TypeSchema schema, string path)
        {
            if (token.Type != expected)
            {
                throw Mismatch(path, $"expected {expected} for {JsonEncodingRules.BranchName(schema)} but found {token.Type}");
            }
        }

        private static AvroTypeMismatchException Mismatch(string path, string message) =>
            new(string.Format(CultureInfo.InvariantCulture, "Invalid Avro JSON at {0}: {1}", path, message));
    }
}
