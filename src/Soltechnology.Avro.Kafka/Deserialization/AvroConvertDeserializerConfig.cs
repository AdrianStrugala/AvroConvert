using Confluent.SchemaRegistry;

namespace SolTechnology.Avro.Kafka.Deserialization;

/// <summary>Configuration of <see cref="AvroConvertSchemaRegistryDeserializer{T}"/>.</summary>
public sealed class AvroConvertDeserializerConfig
{
    /// <summary>How the subject is derived from the topic and record name. Default: <see cref="Confluent.SchemaRegistry.SubjectNameStrategy.Topic"/>.</summary>
    public SubjectNameStrategy SubjectNameStrategy { get; set; } = SubjectNameStrategy.Topic;

    /// <summary>Where the schema id is read from. Default: a message header when present, otherwise the prefix of the data.</summary>
    public SchemaIdDeserializerStrategy SchemaIdStrategy { get; set; } = SchemaIdDeserializerStrategy.Dual;

    /// <summary>Options passed to AvroConvert (naming policy, converters, missing field handling, ...). Also used to generate the reader schema of <c>T</c>.</summary>
    public AvroConvertOptions SerializerOptions { get; set; }
}
