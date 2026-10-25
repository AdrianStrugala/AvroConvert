using System.Collections.Generic;
using Confluent.SchemaRegistry;

namespace SolTechnology.Avro.Kafka.Serialization;

/// <summary>
/// Configuration of <see cref="AvroConvertSchemaRegistrySerializer{T}"/>. The defaults match the Confluent serializers
/// with TopicNameStrategy: the schema of <c>T</c> is registered under <c>{topic}-value</c> / <c>{topic}-key</c> and its id is
/// written in front of the data (magic byte 0 + 4-byte big-endian id).
/// </summary>
public sealed class AvroConvertSerializerConfig
{
    /// <summary>Registers the schema of <c>T</c> when it is not in the registry yet. When false the schema must already be registered.</summary>
    public bool AutoRegisterSchemas { get; set; } = true;

    /// <summary>Asks the registry to normalize the schema before registering or looking it up.</summary>
    public bool NormalizeSchemas { get; set; }

    /// <summary>
    /// Writes with the latest schema registered under the subject instead of the schema of <c>T</c>.
    /// Cannot be combined with <see cref="AutoRegisterSchemas"/>.
    /// </summary>
    public bool UseLatestVersion { get; set; }

    /// <summary>Writes with the latest schema registered under the subject whose metadata contains these entries.</summary>
    public IDictionary<string, string> UseLatestWithMetadata { get; set; }

    /// <summary>Writes with this registered schema id instead of the schema of <c>T</c>.</summary>
    public int? UseSchemaId { get; set; }

    /// <summary>How the subject is derived from the topic and record name. Default: <see cref="Confluent.SchemaRegistry.SubjectNameStrategy.Topic"/>.</summary>
    public SubjectNameStrategy SubjectNameStrategy { get; set; } = SubjectNameStrategy.Topic;

    /// <summary>Where the schema id is written: in front of the data (default) or in a message header.</summary>
    public SchemaIdSerializerStrategy SchemaIdStrategy { get; set; } = SchemaIdSerializerStrategy.Prefix;

    /// <summary>Options passed to AvroConvert (naming policy, converters, ...). Also used to generate the schema of <c>T</c>.</summary>
    public AvroConvertOptions SerializerOptions { get; set; }
}
