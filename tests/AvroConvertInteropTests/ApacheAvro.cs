using System.Numerics;
using Avro;
using Avro.File;
using Avro.File.Snappy;
using Avro.File.Zstandard;
using Avro.Generic;
using Avro.IO;
using Xunit;

namespace AvroConvertInteropTests;

/// <summary>Apache.Avro 1.12 used as the reference implementation.</summary>
internal static class ApacheAvro
{
    static ApacheAvro()
    {
        Codec.RegisterResolver(name => name switch
        {
            "snappy" => new SnappyCodec(),
            "zstandard" => new ZstandardCodec(),
            _ => null
        });
    }

    internal static Codec CreateCodec(string name) => name switch
    {
        "null" => Codec.CreateCodec(Codec.Type.Null),
        "deflate" => Codec.CreateCodec(Codec.Type.Deflate),
        "snappy" => new SnappyCodec(),
        "zstandard" => new ZstandardCodec(),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null)
    };

    /// <summary>Apache's zstandard codec needs the native libzstd; without it the reference implementation cannot be used.</summary>
    internal static void SkipIfCodecUnavailable(string name)
    {
        try
        {
            CreateCodec(name).Compress(new byte[] { 1, 2, 3 });
        }
        catch (DllNotFoundException e)
        {
            Skip.If(true, $"Apache.Avro codec '{name}' is unavailable: {e.Message.Split('.')[0]}");
        }
    }

    internal static byte[] WriteContainer(Schema schema, Codec codec, params GenericRecord[] records)
    {
        using var stream = new MemoryStream();
        using (var writer = DataFileWriter<GenericRecord>.OpenWriter(new GenericDatumWriter<GenericRecord>(schema), stream, codec, leaveOpen: true))
        {
            foreach (var record in records)
            {
                writer.Append(record);
            }
        }
        return stream.ToArray();
    }

    internal static (Schema Schema, List<GenericRecord> Records) ReadContainer(byte[] data) => ReadContainer<GenericRecord>(data);

    internal static (Schema Schema, List<T> Records) ReadContainer<T>(byte[] data)
    {
        using var reader = DataFileReader<T>.OpenReader(new MemoryStream(data));
        return (reader.GetSchema(), reader.NextEntries.ToList());
    }

    internal static byte[] WriteBinary(Schema schema, GenericRecord record)
    {
        using var stream = new MemoryStream();
        new GenericDatumWriter<GenericRecord>(schema).Write(record, new BinaryEncoder(stream));
        return stream.ToArray();
    }

    internal static GenericRecord ReadBinary(Schema schema, byte[] data)
    {
        return new GenericDatumReader<GenericRecord>(schema, schema).Read(null!, new BinaryDecoder(new MemoryStream(data)));
    }

    internal static GenericRecord ToGenericRecord(InteropRecord source, RecordSchema schema)
    {
        var record = new GenericRecord(schema);
        record.Add(nameof(source.IntValue), source.IntValue);
        record.Add(nameof(source.LongValue), source.LongValue);
        record.Add(nameof(source.FloatValue), source.FloatValue);
        record.Add(nameof(source.DoubleValue), source.DoubleValue);
        record.Add(nameof(source.BoolValue), source.BoolValue);
        record.Add(nameof(source.StringValue), source.StringValue);
        record.Add(nameof(source.BytesValue), source.BytesValue);
        record.Add(nameof(source.IntList), source.IntList.Cast<object>().ToArray());
        record.Add(nameof(source.StringMap), source.StringMap.ToDictionary(kv => kv.Key, kv => (object)kv.Value));

        var childSchema = (RecordSchema)schema[nameof(source.Child)].Schema;
        var child = new GenericRecord(childSchema);
        child.Add(nameof(source.Child.Name), source.Child.Name);
        child.Add(nameof(source.Child.Amount), source.Child.Amount);
        record.Add(nameof(source.Child), child);

        record.Add(nameof(source.Color), new GenericEnum((EnumSchema)schema[nameof(source.Color)].Schema, source.Color.ToString()));
        record.Add(nameof(source.NullableInt), source.NullableInt);

        var decimalSchema = (LogicalSchema)schema[nameof(source.DecimalValue)].Schema;
        int scale = decimalSchema.GetProperty("scale") is { } s ? int.Parse(s) : 0;
        record.Add(nameof(source.DecimalValue), new AvroDecimal(new BigInteger(source.DecimalValue * Pow10(scale)), scale));

        record.Add(nameof(source.Timestamp), source.Timestamp);
        record.Add(nameof(source.Id), source.Id);
        record.Add(nameof(source.Date), source.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        record.Add(nameof(source.Time), source.Time.ToTimeSpan());
        return record;
    }

    private static decimal Pow10(int n)
    {
        decimal result = 1m;
        for (int i = 0; i < n; i++) result *= 10m;
        return result;
    }
}
