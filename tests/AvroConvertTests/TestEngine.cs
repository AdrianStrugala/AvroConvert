using System;
using System.Collections.Generic;
using SolTechnology.Avro;

namespace AvroConvertComponentTests;

public static class TestEngine
{
    public static IEnumerable<object[]> All()
    {
        yield return Default;

        yield return Headless;

        yield return Json;

        yield return Brotli;

        yield return Snappy;

        yield return Deflate;

        yield return Gzip;
    }

    public static IEnumerable<object[]> Core()
    {
        yield return Default;

        yield return Headless;

        yield return Brotli;

        yield return Snappy;
    }

    public static IEnumerable<object[]> DefaultOnly()
    {
        yield return Default;

        yield return Snappy;
    }

    private static object[] Default
    {
        get
        {
            var @default = new Func<object, Type, dynamic>((input, type) =>
            {
                var serialized = AvroConvert.Serialize(input);
                return AvroConvert.Deserialize(serialized, type);
            });

            return new object[] { @default };
        }
    }

    private static object[] Headless
    {
        get
        {
            var headless = new Func<object, Type, dynamic>((input, type) =>
            {
                var schema = AvroConvert.GenerateSchema(input.GetType());
                var serialized = AvroConvert.SerializeHeadless(input, schema);
                return AvroConvert.DeserializeHeadless(serialized, type);
            });

            return new object[] { headless };
        }
    }

    private static object[] Json
    {
        get
        {
            var @default = new Func<object, Type, dynamic>((input, type) =>
            {
                var serialized = AvroConvert.Serialize(input);
                var json = AvroConvert.Avro2Json(serialized);
                var avro = AvroConvert.Json2Avro(json);
                return AvroConvert.Deserialize(avro, type);
            });

            return new object[] { @default };
        }
    }

    private static object[] Brotli
    {
        get
        {
            var @default = new Func<object, Type, dynamic>((input, type) =>
            {
                var serialized = AvroConvert.Serialize(input, CodecType.Brotli);
                return AvroConvert.Deserialize(serialized, type);
            });

            return new object[] { @default };
        }
    }

    private static object[] Snappy
    {
        get
        {
            var @default = new Func<object, Type, dynamic>((input, type) =>
            {
                var serialized = AvroConvert.Serialize(input, CodecType.Snappy);
                return AvroConvert.Deserialize(serialized, type);
            });

            return new object[] { @default };
        }
    }

    private static object[] Deflate
    {
        get
        {
            var @default = new Func<object, Type, dynamic>((input, type) =>
            {
                var serialized = AvroConvert.Serialize(input, CodecType.Deflate);
                return AvroConvert.Deserialize(serialized, type);
            });

            return new object[] { @default };
        }
    }


    private static object[] Gzip
    {
        get
        {
            var @default = new Func<object, Type, dynamic>((input, type) =>
            {
                var serialized = AvroConvert.Serialize(input, CodecType.GZip);
                return AvroConvert.Deserialize(serialized, type);
            });

            return new object[] { @default };
        }
    }

}