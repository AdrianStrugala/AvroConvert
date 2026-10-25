using System;
using Confluent.Kafka;

namespace SolTechnology.Avro.Kafka.Deserialization;

/// <summary>
/// Deserializes plain Avro binary (no header, no schema id), as written by <see cref="Serialization.AvroConvertSerializer{T}"/>.
/// For the Confluent Schema Registry wire format use <see cref="AvroConvertSchemaRegistryDeserializer{T}"/>.
/// </summary>
/// <remarks>A null message (tombstone) is returned as <c>default</c>.</remarks>
public class AvroConvertDeserializer<T> : IDeserializer<T>
{
    private readonly AvroSchema _writerSchema;
    private readonly AvroSchema _readerSchema;
    private readonly AvroConvertOptions _options;

    /// <param name="writerSchema">Schema the data was written with; null assumes the schema generated from <typeparamref name="T"/>.</param>
    /// <param name="options">Options passed to AvroConvert.</param>
    public AvroConvertDeserializer(AvroSchema writerSchema = null, AvroConvertOptions options = null)
    {
        _options = options;
        _readerSchema = AvroSchema.Create<T>(options);
        _writerSchema = writerSchema ?? _readerSchema;
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return isNull ? default : AvroConvert.DeserializeHeadless<T>(data, _writerSchema, _readerSchema, _options);
    }
}
