using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SolTechnology.Avro;

namespace AvroConvertComponentTests
{
    /// <summary>The ways a value travels through AvroConvert in component tests.</summary>
    public enum Format
    {
        /// <summary>Object container file, no compression.</summary>
        Container,
        /// <summary>Binary encoding without header; the schema is generated from the target type.</summary>
        Headless,
        /// <summary>Container → Avro2Json → Json2Avro&lt;T&gt; → container.</summary>
        PlainJson,
        Deflate,
        Snappy,
        Zstandard,
        GZip,
        Brotli,
        /// <summary>Container → Avro2JsonEncoding → JsonEncoding2Avro → container (spec JSON Encoding must be lossless).</summary>
        JsonEncoding
    }

    /// <summary>The public ways of writing an object container file.</summary>
    public enum ContainerWriter
    {
        Bytes,
        Stream,
        AsyncStream,
        BufferWriter
    }

    /// <summary>The public ways of reading the entries of an object container file.</summary>
    public enum ContainerReader
    {
        Bytes,
        Span,
        BoxedType,
        AsyncStream,
        LineByLine
    }

    /// <summary>The public ways of reading binary data without a header; the writer schema comes with the call.</summary>
    public enum HeadlessReader
    {
        BytesAndSchemaText,
        SpanAndSchemaText,
        BytesAndAvroSchema,
        BytesAndBothSchemas,
        SpanAndBothSchemas,
        BytesAndBothSchemaTexts,
        BoxedType
    }

    /// <summary>The public ways of pointing at an object container file.</summary>
    public enum ContainerSource
    {
        Bytes,
        Stream
    }

    /// <summary>Format sets for <c>[ValueSource(typeof(Formats), nameof(Formats.X))]</c>.</summary>
    public static class Formats
    {
        public static IEnumerable<Format> All => Enum.GetValues<Format>();

        /// <summary>Formats that write a value whose schema comes from the value itself; headless data has no place for that schema.</summary>
        public static IEnumerable<Format> WithHeader => All.Where(format => format != Format.Headless);

        /// <summary>Every codec a container can be written with.</summary>
        public static IEnumerable<CodecType> Codecs => Enum.GetValues<CodecType>();

        public static IEnumerable<ContainerWriter> ContainerWriters => Enum.GetValues<ContainerWriter>();

        public static IEnumerable<ContainerReader> ContainerReaders => Enum.GetValues<ContainerReader>();

        public static IEnumerable<HeadlessReader> HeadlessReaders => Enum.GetValues<HeadlessReader>();

        public static IEnumerable<ContainerSource> ContainerSources => Enum.GetValues<ContainerSource>();

        /// <summary>Container, headless, plain JSON and two codecs – for scenarios where codecs add no coverage.</summary>
        public static IEnumerable<Format> Core => new[] { Format.Container, Format.Headless, Format.PlainJson, Format.Brotli, Format.Snappy };

        /// <summary>Formats that carry or take a writer schema and resolve it against a reader schema.</summary>
        public static IEnumerable<Format> Evolution => new[] { Format.Container, Format.Headless, Format.Brotli };

        /// <summary>Container only, uncompressed and compressed – for scenarios that depend on the container header.</summary>
        public static IEnumerable<Format> ContainerOnly => new[] { Format.Container, Format.Snappy };

        /// <summary>Ways of reading existing Avro files: <see cref="RoundTrip.Read{T}"/>.</summary>
        public static IEnumerable<Format> Readers => new[] { Format.Container, Format.PlainJson };
    }

    /// <summary>Typed round trips through AvroConvert for every <see cref="Format"/>.</summary>
    public static class RoundTrip
    {
        /// <summary>Writes <paramref name="value"/> and reads it back as <typeparamref name="T"/>.</summary>
        public static T Run<T>(object value, Format format, AvroConvertOptions options = null)
        {
            switch (format)
            {
                case Format.Container:
                    return options == null
                        ? AvroConvert.Deserialize<T>(AvroConvert.Serialize(value))
                        : AvroConvert.Deserialize<T>(AvroConvert.Serialize(value, options), options);
                case Format.Headless:
                {
                    var schema = AvroConvert.GenerateSchema(typeof(T));
                    return AvroConvert.DeserializeHeadless<T>(AvroConvert.SerializeHeadless(value, schema, options), schema);
                }
                case Format.PlainJson:
                    return AvroConvert.Deserialize<T>(AvroConvert.Json2Avro<T>(AvroConvert.Avro2Json(AvroConvert.Serialize(value))));
                case Format.JsonEncoding:
                {
                    var avro = AvroConvert.Serialize(value);
                    var json = AvroConvert.Avro2JsonEncoding(avro);
                    return AvroConvert.Deserialize<T>(AvroConvert.JsonEncoding2Avro(json, AvroConvert.GetSchema(avro)));
                }
                default:
                    return AvroConvert.Deserialize<T>(AvroConvert.Serialize(value, Codec(format)));
            }
        }

        /// <summary>
        /// Writes <paramref name="value"/> with <paramref name="schema"/>, or the schema of its own type, and reads it back as
        /// <typeparamref name="T"/>, a type whose generated schema is not the one the data was written with (a record read into a dictionary).
        /// </summary>
        public static T ReadAs<T>(object value, Format format, AvroSchema schema = null)
        {
            switch (format)
            {
                case Format.Container:
                    return AvroConvert.Deserialize<T>(ToContainer(value, schema));
                case Format.Headless:
                {
                    var schemaText = schema?.Json ?? AvroConvert.GenerateSchema(value.GetType());
                    return AvroConvert.DeserializeHeadless<T>(AvroConvert.SerializeHeadless(value, schemaText), schemaText);
                }
                case Format.PlainJson:
                    // The schema is inferred from the JSON: Json2Avro<T> would write T, which is what ReadAs goes around.
                    return AvroConvert.Deserialize<T>(AvroConvert.Json2Avro(AvroConvert.Avro2Json(ToContainer(value, schema))));
                case Format.JsonEncoding:
                {
                    var avro = ToContainer(value, schema);
                    var json = AvroConvert.Avro2JsonEncoding(avro);
                    return AvroConvert.Deserialize<T>(AvroConvert.JsonEncoding2Avro(json, AvroConvert.GetSchema(avro)));
                }
                default:
                    return AvroConvert.Deserialize<T>(ToContainer(value, schema, Codec(format)));
            }
        }

        private static byte[] ToContainer(object value, AvroSchema schema, CodecType codec = CodecType.Null) =>
            schema == null ? AvroConvert.Serialize(value, codec) : AvroConvert.Serialize(value, schema, new AvroConvertOptions { Codec = codec });

        /// <summary>Same as <see cref="ReadAs{T}"/> for a target type known only at runtime.</summary>
        public static object ReadAs(object value, Type type, Format format, AvroSchema schema = null) =>
            typeof(RoundTrip).GetMethods().Single(m => m.Name == nameof(ReadAs) && m.IsGenericMethod)
                .MakeGenericMethod(type)
                .Invoke(null, new[] { value, format, schema });

        /// <summary>An uncompressed container built byte by byte, for data no public writer would produce.</summary>
        public static byte[] RawContainer(string schema, byte[] data, long entries)
        {
            var output = new List<byte> { (byte)'O', (byte)'b', (byte)'j', 1 };
            var schemaBytes = System.Text.Encoding.UTF8.GetBytes(schema);
            var codecBytes = System.Text.Encoding.UTF8.GetBytes("null");
            var schemaKey = System.Text.Encoding.UTF8.GetBytes("avro.schema");
            var codecKey = System.Text.Encoding.UTF8.GetBytes("avro.codec");

            output.AddRange(ZigZag(2));
            foreach (var part in new[] { schemaKey, schemaBytes, codecKey, codecBytes })
            {
                output.AddRange(ZigZag(part.Length));
                output.AddRange(part);
            }

            output.AddRange(ZigZag(0));
            var sync = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
            output.AddRange(sync);
            output.AddRange(ZigZag(entries));
            output.AddRange(ZigZag(data.Length));
            output.AddRange(data);
            output.AddRange(sync);
            return output.ToArray();
        }

        /// <summary>A long as Avro encodes it.</summary>
        public static byte[] ZigZag(long value)
        {
            var result = new List<byte>();
            var encoded = (ulong)((value << 1) ^ (value >> 63));
            while ((encoded & ~0x7FUL) != 0)
            {
                result.Add((byte)((encoded & 0x7F) | 0x80));
                encoded >>= 7;
            }

            result.Add((byte)encoded);
            return result.ToArray();
        }

        /// <summary>Writes <paramref name="items"/> as the entries of an object container file.</summary>
        public static byte[] Write<T>(ContainerWriter writer, IEnumerable<T> items, AvroConvertOptions options = null)
        {
            var list = items.ToList();
            switch (writer)
            {
                case ContainerWriter.Bytes:
                    return AvroConvert.Serialize(list, options ?? new AvroConvertOptions());
                case ContainerWriter.Stream:
                {
                    using var output = new MemoryStream();
                    AvroConvert.Serialize<T>(list, output, options);
                    return output.ToArray();
                }
                case ContainerWriter.AsyncStream:
                {
                    using var output = new MemoryStream();
                    AvroConvert.Serialize(ToAsync(list), output, options).GetAwaiter().GetResult();
                    return output.ToArray();
                }
                case ContainerWriter.BufferWriter:
                {
                    var output = new ArrayBufferWriter<byte>();
                    AvroConvert.Serialize(list, output, options);
                    return output.WrittenSpan.ToArray();
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(writer), writer, null);
            }
        }

        /// <summary>Writes <paramref name="items"/> as the entries of an object container file with the given schema.</summary>
        public static byte[] Write<T>(ContainerWriter writer, IEnumerable<T> items, AvroSchema schema, AvroConvertOptions options = null, IReadOnlyDictionary<string, AvroMetadataValue> metadata = null)
        {
            var list = items.ToList();
            switch (writer)
            {
                case ContainerWriter.Bytes:
                    return AvroConvert.Serialize(list, schema, options, metadata);
                case ContainerWriter.Stream:
                {
                    using var output = new MemoryStream();
                    AvroConvert.Serialize<T>(list, output, schema, options, metadata);
                    return output.ToArray();
                }
                case ContainerWriter.AsyncStream:
                {
                    using var output = new MemoryStream();
                    AvroConvert.Serialize(ToAsync(list), output, schema, options, metadata).GetAwaiter().GetResult();
                    return output.ToArray();
                }
                case ContainerWriter.BufferWriter:
                {
                    var output = new ArrayBufferWriter<byte>();
                    AvroConvert.Serialize(list, output, schema, options, metadata);
                    return output.WrittenSpan.ToArray();
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(writer), writer, null);
            }
        }

        /// <summary>Reads every entry of an object container file as <typeparamref name="T"/>.</summary>
        public static List<T> ReadAll<T>(ContainerReader reader, byte[] avro)
        {
            switch (reader)
            {
                case ContainerReader.Bytes:
                    return AvroConvert.Deserialize<List<T>>(avro);
                case ContainerReader.Span:
                    return AvroConvert.Deserialize<List<T>>(new ReadOnlySpan<byte>(avro));
                case ContainerReader.BoxedType:
                    return (List<T>)AvroConvert.Deserialize(avro, typeof(List<T>));
                case ContainerReader.AsyncStream:
                    return ToList(AvroConvert.Deserialize<T>(new MemoryStream(avro))).GetAwaiter().GetResult();
                case ContainerReader.LineByLine:
                {
                    using var lines = AvroConvert.OpenDeserializer<T>(new MemoryStream(avro));
                    var result = new List<T>();
                    while (lines.HasNext())
                    {
                        result.Add(lines.ReadNext());
                    }

                    return result;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(reader), reader, null);
            }
        }

        /// <summary>Reads one value of binary data without a header, written with <paramref name="writerSchema"/>.</summary>
        public static T ReadHeadless<T>(HeadlessReader reader, byte[] avro, string writerSchema)
        {
            var readerSchema = AvroConvert.GenerateSchema(typeof(T));
            return reader switch
            {
                HeadlessReader.BytesAndSchemaText => AvroConvert.DeserializeHeadless<T>(avro, writerSchema),
                HeadlessReader.SpanAndSchemaText => AvroConvert.DeserializeHeadless<T>(new ReadOnlySpan<byte>(avro), writerSchema),
                HeadlessReader.BytesAndAvroSchema => AvroConvert.DeserializeHeadless<T>(avro, AvroSchema.Parse(writerSchema)),
                HeadlessReader.BytesAndBothSchemas => AvroConvert.DeserializeHeadless<T>(avro, AvroSchema.Parse(writerSchema), AvroSchema.Parse(readerSchema)),
                HeadlessReader.SpanAndBothSchemas => AvroConvert.DeserializeHeadless<T>(new ReadOnlySpan<byte>(avro), AvroSchema.Parse(writerSchema), AvroSchema.Parse(readerSchema)),
                HeadlessReader.BytesAndBothSchemaTexts => AvroConvert.DeserializeHeadless<T>(avro, writerSchema, readerSchema, 1),
                HeadlessReader.BoxedType => (T)AvroConvert.DeserializeHeadless(avro, writerSchema, typeof(T)),
                _ => throw new ArgumentOutOfRangeException(nameof(reader), reader, null)
            };
        }

        /// <summary>Runs the read on a file, in the way <paramref name="source"/> points at it.</summary>
        public static T FromSource<T>(ContainerSource source, byte[] avro, Func<byte[], T> fromBytes, Func<Stream, T> fromStream) =>
            source switch
            {
                ContainerSource.Bytes => fromBytes(avro),
                ContainerSource.Stream => fromStream(new MemoryStream(avro)),
                _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
            };

        private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                await Task.Yield();
                yield return item;
            }
        }

        private static async Task<List<T>> ToList<T>(IAsyncEnumerable<T> items)
        {
            var result = new List<T>();
            await foreach (var item in items)
            {
                result.Add(item);
            }

            return result;
        }

        /// <summary>Same as <see cref="Run{T}"/> for a target type known only at runtime.</summary>
        public static object Run(object value, Type type, Format format, AvroConvertOptions options = null) =>
            typeof(RoundTrip).GetMethods().Single(m => m.Name == nameof(Run) && m.IsGenericMethod)
                .MakeGenericMethod(type)
                .Invoke(null, new[] { value, format, options });

        /// <summary>
        /// Schema evolution: <paramref name="value"/> is written with its own schema (or <paramref name="writerSchema"/>) and read
        /// with the schema of <typeparamref name="TReader"/> (or <paramref name="readerSchema"/>) through Avro schema resolution.
        /// </summary>
        public static TReader Evolve<TReader>(object value, Format format, string writerSchema = null, string readerSchema = null)
        {
            switch (format)
            {
                case Format.Headless:
                {
                    writerSchema ??= AvroConvert.GenerateSchema(value.GetType());
                    readerSchema ??= AvroConvert.GenerateSchema(typeof(TReader));
                    var avro = AvroConvert.SerializeHeadless(value, writerSchema);
                    return AvroConvert.DeserializeHeadless<TReader>(avro, writerSchema, readerSchema, 1);
                }
                case Format.Container:
                case Format.Deflate:
                case Format.Snappy:
                case Format.Zstandard:
                case Format.GZip:
                case Format.Brotli:
                    // The container carries the writer schema; the reader schema comes from TReader.
                    return AvroConvert.Deserialize<TReader>(format == Format.Container ? AvroConvert.Serialize(value) : AvroConvert.Serialize(value, Codec(format)));
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, "Not a schema evolution format");
            }
        }

        /// <summary>Reads existing Avro data as <typeparamref name="T"/>.</summary>
        public static T Read<T>(byte[] avro, Format format) => format switch
        {
            Format.Container => AvroConvert.Deserialize<T>(avro),
            Format.PlainJson => JsonConvert.DeserializeObject<T>(AvroConvert.Avro2Json(avro)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Not a reader format")
        };

        private static CodecType Codec(Format format) => format switch
        {
            Format.Deflate => CodecType.Deflate,
            Format.Snappy => CodecType.Snappy,
            Format.Zstandard => CodecType.Zstandard,
            Format.GZip => CodecType.GZip,
            Format.Brotli => CodecType.Brotli,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Not a codec format")
        };
    }

    /// <summary>Exception thrown by an action, or null.</summary>
    public static class Recorded
    {
        public static Exception Exception(Action action)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public static Exception Exception(Func<object> func) => Exception(() => { func(); });
    }
}
