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
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.Features.JsonEncoding
{
    /// <summary>
    /// Avro binary → JSON as defined in the "JSON Encoding" section of the Avro specification. Every value is decoded with the
    /// writer schema; several values (container entries, or consecutive headless values) are separated by new lines.
    /// </summary>
    internal sealed class JsonEncodingWriter
    {
        [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
        internal string Convert(byte[] avro, TypeSchema headlessSchema)
        {
            using var stream = new MemoryStream(avro, writable: false);
            using var text = new StringWriter(CultureInfo.InvariantCulture);

            if (StartsWithHeader(avro))
            {
                var reader = new Reader(stream);
                reader.ReadFixed(new byte[DataFileConstants.AvroHeader.Length]);
                var header = reader.ReadHeader();
                var schema = Schema.Parse(header.GetMetadata(DataFileConstants.SchemaMetadataKey));
                reader.ReadFixed(header.SyncData);
                var codec = AbstractCodec.CreateCodecFromString(header.GetMetadata(DataFileConstants.CodecMetadataKey));

                var first = true;
                while (!reader.IsReadToEnd())
                {
                    long count = reader.ReadLong();
                    var block = new Reader(reader.ReadDataBlock(header.SyncData, codec));
                    for (long i = 0; i < count; i++)
                    {
                        WriteSeparator(text, ref first);
                        WriteValue(schema, block, text);
                    }
                }
            }
            else
            {
                if (headlessSchema == null)
                {
                    throw new MissingSchemaException("Provide valid schema for the Avro data");
                }

                var reader = new Reader(stream);
                var first = true;
                do
                {
                    WriteSeparator(text, ref first);
                    WriteValue(headlessSchema, reader, text);
                } while (stream.Position < stream.Length);
            }

            return text.ToString();
        }

        internal static JsonTextWriter CreateWriter(TextWriter text) => new(text)
        {
            Formatting = Formatting.None,
            // NaN and infinities have no JSON number form; they are written as strings like other Avro implementations do.
            FloatFormatHandling = FloatFormatHandling.String,
            CloseOutput = false
        };

        private static void WriteValue(TypeSchema schema, IReader reader, TextWriter text)
        {
            // One JSON writer per top-level value: the values are separated by new lines, not commas.
            using var json = CreateWriter(text);
            Write(schema, reader, json);
            json.Flush();
        }

        private static bool StartsWithHeader(byte[] avro) =>
            avro.Length >= DataFileConstants.AvroHeader.Length && avro.AsSpan(0, DataFileConstants.AvroHeader.Length).SequenceEqual(DataFileConstants.AvroHeader);

        private static void WriteSeparator(TextWriter text, ref bool first)
        {
            if (!first)
            {
                text.Write('\n');
            }
            first = false;
        }

        internal static void Write(TypeSchema schema, IReader reader, JsonWriter json)
        {
            switch (schema)
            {
                case LogicalTypeSchema logical:
                    Write(logical.BaseTypeSchema, reader, json);
                    return;
                case NullableSchema nullable:
                    Write(nullable.ValueSchema, reader, json);
                    return;
            }

            switch (schema.Type)
            {
                case AvroType.Null:
                    reader.ReadNull();
                    json.WriteNull();
                    break;
                case AvroType.Boolean:
                    json.WriteValue(reader.ReadBoolean());
                    break;
                case AvroType.Int:
                    json.WriteValue(reader.ReadInt());
                    break;
                case AvroType.Long:
                    json.WriteValue(reader.ReadLong());
                    break;
                case AvroType.Float:
                    json.WriteValue(reader.ReadFloat());
                    break;
                case AvroType.Double:
                    json.WriteValue(reader.ReadDouble());
                    break;
                case AvroType.Bytes:
                    json.WriteValue(JsonEncodingRules.Bytes.GetString(reader.ReadBytes()));
                    break;
                case AvroType.String:
                    json.WriteValue(reader.ReadString());
                    break;
                case AvroType.Record:
                case AvroType.Error:
                    json.WriteStartObject();
                    foreach (var field in ((RecordSchema)schema).Fields)
                    {
                        json.WritePropertyName(field.Name);
                        Write(field.TypeSchema, reader, json);
                    }
                    json.WriteEndObject();
                    break;
                case AvroType.Enum:
                    var enumSchema = (EnumSchema)schema;
                    var position = reader.ReadEnum();
                    if (position < 0 || position >= enumSchema.Symbols.Count)
                    {
                        throw new AvroTypeMismatchException($"Enum position {position} out of range for {enumSchema.FullName}");
                    }
                    json.WriteValue(enumSchema.Symbols[position]);
                    break;
                case AvroType.Fixed:
                    var buffer = new byte[((FixedSchema)schema).Size];
                    reader.ReadFixed(buffer);
                    json.WriteValue(JsonEncodingRules.Bytes.GetString(buffer));
                    break;
                case AvroType.Array:
                    var itemSchema = ((ArraySchema)schema).ItemSchema;
                    json.WriteStartArray();
                    for (long n = reader.ReadArrayStart(); n != 0; n = reader.ReadArrayNext())
                    {
                        for (long i = 0; i < n; i++)
                        {
                            Write(itemSchema, reader, json);
                        }
                    }
                    json.WriteEndArray();
                    break;
                case AvroType.Map:
                    var valueSchema = ((MapSchema)schema).ValueSchema;
                    json.WriteStartObject();
                    for (long n = reader.ReadMapStart(); n != 0; n = reader.ReadMapNext())
                    {
                        for (long i = 0; i < n; i++)
                        {
                            json.WritePropertyName(reader.ReadString());
                            Write(valueSchema, reader, json);
                        }
                    }
                    json.WriteEndObject();
                    break;
                case AvroType.Union:
                    var branches = ((UnionSchema)schema).Schemas;
                    var index = reader.ReadUnionIndex();
                    if (index < 0 || index >= branches.Count)
                    {
                        throw new AvroTypeMismatchException($"Union index {index} out of range; the union has {branches.Count} branches");
                    }

                    var branch = branches[index];
                    if (JsonEncodingRules.Unwrap(branch).Type == AvroType.Null)
                    {
                        reader.ReadNull();
                        json.WriteNull();
                    }
                    else
                    {
                        json.WriteStartObject();
                        json.WritePropertyName(JsonEncodingRules.BranchName(branch));
                        Write(branch, reader, json);
                        json.WriteEndObject();
                    }
                    break;
                default:
                    throw new AvroException($"Unknown schema type: {schema.Type}");
            }
        }
    }
}
