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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.FileHeader;
using SolTechnology.Avro.AvroObjectServices.FileHeader.Codec;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.Features.AvroToJson
{
    /// <summary>
    /// Avro2Json: writes the JSON while it reads the data, without building the records as objects first. Records and maps
    /// are objects, arrays are arrays, unions are their value; the values are written as Newtonsoft writes them with its
    /// default settings (JsonConvert.DefaultSettings of the application are not used: the names are the schema's).
    /// </summary>
    /// <remarks>
    /// Every record, array and map checks the stack, and nothing catches on the way up. Newtonsoft's own recursion over the
    /// decoded objects had no check and caught at every level to build the JSON path, so data the decoder still read
    /// (about 1,200 levels on a 1 MB stack) overflowed the stack, which ends the process.
    /// </remarks>
    [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
    internal sealed class JsonOutput
    {
        private readonly JsonWriter _writer;

        /// <summary>The decoder of the values that are written as one JSON value: logical types, enums, fixed, dictionary keys.</summary>
        private readonly Resolver _values;

        private JsonOutput(JsonWriter writer, TypeSchema schema)
        {
            _writer = writer;
            _values = new Resolver(schema);
        }

        /// <summary>A container file (one value, or an array of its entries) or, with <paramref name="schema"/>, headless data.</summary>
        internal static string Write(Stream stream, TypeSchema schema)
        {
            var text = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture);
            using (var writer = new JsonTextWriter(text))
            {
                WriteData(writer, stream, schema);
            }

            return text.ToString();
        }

        private static void WriteData(JsonWriter writer, Stream stream, TypeSchema schema)
        {
            var reader = new Reader(stream);
            byte[] firstBytes = new byte[DataFileConstants.AvroHeader.Length];
            try
            {
                reader.ReadFixed(firstBytes);
            }
            catch (EndOfStreamException)
            {
                // shorter than the header
            }

            if (!firstBytes.SequenceEqual(DataFileConstants.AvroHeader))
            {
                if (schema == null)
                {
                    throw new MissingSchemaException("Provide valid schema for the Avro data");
                }

                stream.Seek(0, SeekOrigin.Begin);
                new JsonOutput(writer, schema).WriteValue(schema, reader);
                return;
            }

            var header = reader.ReadHeader();
            schema ??= Schema.Parse(header.GetMetadata(DataFileConstants.SchemaMetadataKey));
            reader.ReadFixed(header.SyncData);
            var codec = AbstractCodec.CreateCodecFromString(header.GetMetadata(DataFileConstants.CodecMetadataKey));

            if (reader.IsReadToEnd())
            {
                writer.WriteValue(string.Empty);
                return;
            }

            new JsonOutput(writer, schema).WriteEntries(schema, header, Blocks(reader, header, codec));
        }

        /// <summary>One entry is the value itself (null for none), more entries – or a file of arrays – are an array of them.</summary>
        private void WriteEntries(TypeSchema schema, Header header, IEnumerable<(long Count, Reader Data)> blocks)
        {
            using var next = blocks.GetEnumerator();
            var pending = new List<(long Count, Reader Data)>();
            if (next.MoveNext())
            {
                pending.Add(next.Current);
                if (next.Current.Count == 1 && next.MoveNext())
                {
                    // The next block is read ahead (not its entries) to tell one entry from more.
                    pending.Add(next.Current);
                }
            }

            long known = pending.Sum(block => block.Count);
            if (header.Schema is not ArraySchema && known < 2)
            {
                if (known == 0)
                {
                    _writer.WriteNull();
                }
                else
                {
                    WriteValue(schema, pending[0].Data);
                }
                return;
            }

            _writer.WriteStartArray();
            foreach (var (count, data) in pending.Concat(Rest(next)))
            {
                for (long i = 0; i < count; i++)
                {
                    WriteValue(schema, data);
                }
            }
            _writer.WriteEndArray();
        }

        private static IEnumerable<(long Count, Reader Data)> Rest(IEnumerator<(long Count, Reader Data)> blocks)
        {
            while (blocks.MoveNext())
            {
                yield return blocks.Current;
            }
        }

        /// <summary>The data blocks that have entries, each decompressed into a reader of its own.</summary>
        private static IEnumerable<(long Count, Reader Data)> Blocks(Reader reader, Header header, AbstractCodec codec)
        {
            while (!reader.IsReadToEnd())
            {
                long count = reader.ReadLong();
                var data = reader.ReadDataBlock(header.SyncData, codec);
                if (count > 0)
                {
                    yield return (count, new Reader(data));
                }
            }
        }

        private void WriteValue(TypeSchema schema, IReader reader)
        {
            switch (schema.Type)
            {
                case AvroType.Null:
                    _writer.WriteNull();
                    return;
                case AvroType.Boolean:
                    _writer.WriteValue(reader.ReadBoolean());
                    return;
                case AvroType.Int:
                    _writer.WriteValue(reader.ReadInt());
                    return;
                case AvroType.Long:
                    _writer.WriteValue(reader.ReadLong());
                    return;
                case AvroType.Float:
                    _writer.WriteValue(reader.ReadFloat());
                    return;
                case AvroType.Double:
                    _writer.WriteValue(reader.ReadDouble());
                    return;
                case AvroType.String:
                    _writer.WriteValue(reader.ReadString());
                    return;
                case AvroType.Bytes:
                    _writer.WriteValue(reader.ReadBytes());
                    return;
                case AvroType.Error:
                case AvroType.Record:
                    WriteRecord((RecordSchema)schema, reader);
                    return;
                case AvroType.Array:
                    WriteArray(((ArraySchema)schema).ItemSchema, reader);
                    return;
                case AvroType.Map:
                    WriteMap((MapSchema)schema, reader);
                    return;
                case AvroType.Union:
                    WriteValue(((UnionSchema)schema).Schemas[reader.ReadUnionIndex()], reader);
                    return;
                case AvroType.Enum:
                case AvroType.Fixed:
                case AvroType.Logical:
                    WriteLeaf(_writer, _values.Resolve(schema, reader));
                    return;
                default:
                    throw new AvroException("Unknown schema type: " + schema);
            }
        }

        private void WriteRecord(RecordSchema schema, IReader reader)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            _writer.WriteStartObject();
            foreach (var field in schema.Fields)
            {
                _writer.WritePropertyName(field.Name);
                WriteValue(field.TypeSchema, reader);
            }
            _writer.WriteEndObject();
        }

        private void WriteArray(TypeSchema itemSchema, IReader reader)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            if (itemSchema.Name == "KeyValuePair2")
            {
                // A .NET dictionary with keys other than string: an object named by its keys.
                WriteKeyValuePairs((RecordSchema)itemSchema, reader);
                return;
            }

            _writer.WriteStartArray();
            for (long n = reader.ReadArrayStart(); n != 0; n = reader.ReadArrayNext())
            {
                for (long i = 0; i < n; i++)
                {
                    WriteValue(itemSchema, reader);
                }
            }
            _writer.WriteEndArray();
        }

        private void WriteKeyValuePairs(RecordSchema pairSchema, IReader reader)
        {
            var keySchema = pairSchema.GetField("Key").TypeSchema;
            var valueSchema = pairSchema.GetField("Value").TypeSchema;
            var keys = new HashSet<object>();

            _writer.WriteStartObject();
            for (long n = reader.ReadArrayStart(); n != 0; n = reader.ReadArrayNext())
            {
                for (long i = 0; i < n; i++)
                {
                    var key = _values.Resolve(keySchema, reader);
                    if (!keys.Add(key))
                    {
                        throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
                    }

                    _writer.WritePropertyName(PropertyName(key));
                    WriteValue(valueSchema, reader);
                }
            }
            _writer.WriteEndObject();
        }

        private void WriteMap(MapSchema schema, IReader reader)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            _writer.WriteStartObject();
            for (long n = reader.ReadMapStart(); n != 0; n = reader.ReadMapNext())
            {
                for (long i = 0; i < n; i++)
                {
                    _writer.WritePropertyName(reader.ReadString());
                    WriteValue(schema.ValueSchema, reader);
                }
            }
            _writer.WriteEndObject();
        }

        /// <summary>A value of a logical type, an enum symbol or fixed bytes, written as Newtonsoft writes it.</summary>
        private static void WriteLeaf(JsonWriter writer, object value)
        {
            switch (value)
            {
                case null:
                    writer.WriteNull();
                    return;
                case string text:
                    writer.WriteValue(text);
                    return;
                case byte[] bytes:
                    writer.WriteValue(bytes);
                    return;
                case decimal number:
                    writer.WriteValue(number);
                    return;
                case DateTime date:
                    writer.WriteValue(date);
                    return;
                case DateTimeOffset date:
                    writer.WriteValue(date);
                    return;
                case Guid guid:
                    writer.WriteValue(guid);
                    return;
                case TimeSpan time:
                    writer.WriteValue(time);
                    return;
                default:
                    // DateOnly, TimeOnly, numbers of logical types on int and long, and anything else.
                    Json.Serializer.Serialize(writer, value);
                    return;
            }
        }

        /// <summary>The name Newtonsoft gives a dictionary key that is not a string (a date in ISO 8601, a number in the invariant culture…).</summary>
        private static string PropertyName(object key)
        {
            switch (key)
            {
                case string name:
                    return name;
                // Newtonsoft names these with Convert.ToString in the invariant culture; dates and floating point have formats of their own.
                case Guid:
                case IConvertible and not (DateTime or double or float or Enum):
                    return Convert.ToString(key, CultureInfo.InvariantCulture);
            }

            var json = new StringWriter(CultureInfo.InvariantCulture);
            Json.Serializer.Serialize(json, new Dictionary<object, object> { [key] = null });
            using var reader = new JsonTextReader(new StringReader(json.ToString()));
            reader.Read();
            reader.Read();
            return (string)reader.Value;
        }
    }
}
