using System;
using System.Buffers;
using Confluent.Kafka;

namespace SolTechnology.Avro.Kafka.Serialization;

/// <summary>
/// Serializes to plain Avro binary (no header, no schema id) for topics whose schema is agreed out of band.
/// For the Confluent Schema Registry wire format use <see cref="AvroConvertSchemaRegistrySerializer{T}"/>.
/// </summary>
/// <remarks>A null value is written as a null message (a tombstone on compacted topics).</remarks>
public class AvroConvertSerializer<T> : ISerializer<T>
{
    private readonly AvroSchema _schema;
    private readonly AvroConvertOptions _options;

    /// <param name="schema">Schema to write with; null uses the schema generated from <typeparamref name="T"/>.</param>
    /// <param name="options">Options passed to AvroConvert.</param>
    public AvroConvertSerializer(AvroSchema schema = null, AvroConvertOptions options = null)
    {
        _options = options;
        _schema = schema ?? AvroSchema.Create<T>(options);
    }

    public AvroSchema Schema => _schema;

    public byte[] Serialize(T data, SerializationContext context)
    {
        if (data == null)
        {
            return null;
        }

        var buffer = new ArrayBufferWriter<byte>();
        AvroConvert.SerializeHeadless(data, buffer, _schema, _options);
        return buffer.WrittenSpan.ToArray();
    }
}
