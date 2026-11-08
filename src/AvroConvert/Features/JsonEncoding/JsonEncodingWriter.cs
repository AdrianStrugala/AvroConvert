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

        /// <remarks>
        /// Recursion goes through here for every level of the data, so this method stays small: the containers have methods of
        /// their own and the exceptions are built elsewhere. One method with all of them took about 3 times the stack per level
        /// (348 levels on a 1 MB stack, 48 on 256 KB), as the JIT reserves room for every local and interpolated message.
        /// </remarks>
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
                    return;
                case AvroType.Boolean:
                    json.WriteValue(reader.ReadBoolean());
                    return;
                case AvroType.Int:
                    json.WriteValue(reader.ReadInt());
                    return;
                case AvroType.Long:
                    json.WriteValue(reader.ReadLong());
                    return;
                case AvroType.Float:
                    json.WriteValue(reader.ReadFloat());
                    return;
                case AvroType.Double:
                    json.WriteValue(reader.ReadDouble());
                    return;
                case AvroType.Bytes:
                    json.WriteValue(JsonEncodingRules.Bytes.GetString(reader.ReadBytes()));
                    return;
                case AvroType.String:
                    json.WriteValue(reader.ReadString());
                    return;
                case AvroType.Record:
                case AvroType.Error:
                    WriteRecord((RecordSchema)schema, reader, json);
                    return;
                case AvroType.Enum:
                    WriteEnum((EnumSchema)schema, reader, json);
                    return;
                case AvroType.Fixed:
                    WriteFixed((FixedSchema)schema, reader, json);
                    return;
                case AvroType.Array:
                    WriteArray((ArraySchema)schema, reader, json);
                    return;
                case AvroType.Map:
                    WriteMap((MapSchema)schema, reader, json);
                    return;
                case AvroType.Union:
                    WriteUnion((UnionSchema)schema, reader, json);
                    return;
                default:
                    throw UnknownType(schema);
            }
        }

        private static void WriteRecord(RecordSchema schema, IReader reader, JsonWriter json)
        {
            System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
            json.WriteStartObject();
            var fields = schema.Fields;
            for (int i = 0; i < fields.Count; i++)
            {
                json.WritePropertyName(fields[i].Name);
                Write(fields[i].TypeSchema, reader, json);
            }
            json.WriteEndObject();
        }

        private static void WriteEnum(EnumSchema schema, IReader reader, JsonWriter json)
        {
            var position = reader.ReadEnum();
            if (position < 0 || position >= schema.Symbols.Count)
            {
                throw EnumOutOfRange(schema, position);
            }
            json.WriteValue(schema.Symbols[position]);
        }

        private static void WriteFixed(FixedSchema schema, IReader reader, JsonWriter json)
        {
            var buffer = new byte[schema.Size];
            reader.ReadFixed(buffer);
            json.WriteValue(JsonEncodingRules.Bytes.GetString(buffer));
        }

        private static void WriteArray(ArraySchema schema, IReader reader, JsonWriter json)
        {
            System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
            var itemSchema = schema.ItemSchema;
            json.WriteStartArray();
            for (long n = reader.ReadArrayStart(); n != 0; n = reader.ReadArrayNext())
            {
                for (long i = 0; i < n; i++)
                {
                    Write(itemSchema, reader, json);
                }
            }
            json.WriteEndArray();
        }

        private static void WriteMap(MapSchema schema, IReader reader, JsonWriter json)
        {
            System.Runtime.CompilerServices.RuntimeHelpers.EnsureSufficientExecutionStack();
            var valueSchema = schema.ValueSchema;
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
        }

        private static void WriteUnion(UnionSchema schema, IReader reader, JsonWriter json)
        {
            var branches = schema.Schemas;
            var index = reader.ReadUnionIndex();
            if (index < 0 || index >= branches.Count)
            {
                throw UnionOutOfRange(branches.Count, index);
            }

            var branch = branches[index];
            if (JsonEncodingRules.Unwrap(branch).Type == AvroType.Null)
            {
                reader.ReadNull();
                json.WriteNull();
                return;
            }

            json.WriteStartObject();
            json.WritePropertyName(JsonEncodingRules.BranchName(branch));
            Write(branch, reader, json);
            json.WriteEndObject();
        }

        private static Exception UnknownType(TypeSchema schema) => new AvroException($"Unknown schema type: {schema.Type}");

        private static Exception EnumOutOfRange(EnumSchema schema, int position) =>
            new AvroTypeMismatchException($"Enum position {position} out of range for {schema.FullName}");

        private static Exception UnionOutOfRange(int branches, int index) =>
            new AvroTypeMismatchException($"Union index {index} out of range; the union has {branches} branches");
    }
}
