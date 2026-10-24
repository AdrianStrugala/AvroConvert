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
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace SolTechnology.Avro.AvroObjectServices.BuildSchema
{
    /// <summary>
    /// "Parsing Canonical Form for Schemas" and the schema fingerprints defined by the Avro specification.
    /// </summary>
    internal static class ParsingCanonicalForm
    {
        /// <summary>CRC-64-AVRO seed (the "empty" fingerprint).</summary>
        private const ulong Empty = 0xc15d213aa4d7a795UL;

        private static readonly ulong[] Table = BuildTable();

        internal static string From(TypeSchema schema)
        {
            using var text = new StringWriter(CultureInfo.InvariantCulture);
            using var writer = new JsonTextWriter(text) { Formatting = Formatting.None };
            Write(schema, writer, new HashSet<string>());
            writer.Flush();
            return text.ToString();
        }

        internal static long Crc64(string canonicalForm)
        {
            ulong fp = Empty;
            foreach (var b in Encoding.UTF8.GetBytes(canonicalForm))
            {
                fp = (fp >> 8) ^ Table[(int)(fp ^ b) & 0xff];
            }

            return unchecked((long)fp);
        }

        internal static byte[] Md5(string canonicalForm) => MD5.HashData(Encoding.UTF8.GetBytes(canonicalForm));

        internal static byte[] Sha256(string canonicalForm) => SHA256.HashData(Encoding.UTF8.GetBytes(canonicalForm));

        private static ulong[] BuildTable()
        {
            var table = new ulong[256];
            for (int i = 0; i < 256; i++)
            {
                ulong fp = (ulong)i;
                for (int j = 0; j < 8; j++)
                {
                    fp = (fp >> 1) ^ (Empty & (0UL - (fp & 1UL)));
                }

                table[i] = fp;
            }

            return table;
        }

        private static void Write(TypeSchema schema, JsonTextWriter writer, HashSet<string> seen)
        {
            switch (schema)
            {
                case NullableSchema nullable:
                    Write(nullable.ValueSchema, writer, seen);
                    return;
                case LogicalTypeSchema logical:
                    // [STRIP] logicalType and its attributes are not part of the canonical form.
                    Write(logical.BaseTypeSchema, writer, seen);
                    return;
                case UnionSchema union:
                    writer.WriteStartArray();
                    foreach (var branch in union.Schemas)
                    {
                        Write(branch, writer, seen);
                    }
                    writer.WriteEndArray();
                    return;
                case ArraySchema array:
                    writer.WriteStartObject();
                    writer.WritePropertyName("type");
                    writer.WriteValue("array");
                    writer.WritePropertyName("items");
                    Write(array.ItemSchema, writer, seen);
                    writer.WriteEndObject();
                    return;
                case MapSchema map:
                    writer.WriteStartObject();
                    writer.WritePropertyName("type");
                    writer.WriteValue("map");
                    writer.WritePropertyName("values");
                    Write(map.ValueSchema, writer, seen);
                    writer.WriteEndObject();
                    return;
                case NamedSchema named when !seen.Add(named.FullName):
                    writer.WriteValue(named.FullName);
                    return;
                case RecordSchema record:
                    writer.WriteStartObject();
                    WriteNameAndType(writer, record.FullName, "record");
                    writer.WritePropertyName("fields");
                    writer.WriteStartArray();
                    foreach (var field in record.Fields)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("name");
                        writer.WriteValue(field.Name);
                        writer.WritePropertyName("type");
                        Write(field.TypeSchema, writer, seen);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    return;
                case EnumSchema enumSchema:
                    writer.WriteStartObject();
                    WriteNameAndType(writer, enumSchema.FullName, "enum");
                    writer.WritePropertyName("symbols");
                    writer.WriteStartArray();
                    foreach (var symbol in enumSchema.Symbols)
                    {
                        writer.WriteValue(symbol);
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    return;
                case FixedSchema fixedSchema:
                    writer.WriteStartObject();
                    WriteNameAndType(writer, fixedSchema.FullName, "fixed");
                    writer.WritePropertyName("size");
                    writer.WriteValue(fixedSchema.Size);
                    writer.WriteEndObject();
                    return;
                default:
                    // [PRIMITIVES] simple form, e.g. "int".
                    writer.WriteValue(schema.Type.ToString().ToLowerInvariant());
                    return;
            }
        }

        private static void WriteNameAndType(JsonTextWriter writer, string fullName, string type)
        {
            writer.WritePropertyName("name");
            writer.WriteValue(fullName);
            writer.WritePropertyName("type");
            writer.WriteValue(type);
        }
    }
}
