using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>Format sets for <c>[ValueSource(typeof(Formats), nameof(Formats.X))]</c>.</summary>
    public static class Formats
    {
        public static IEnumerable<Format> All => Enum.GetValues<Format>();

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
