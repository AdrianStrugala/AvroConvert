using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;

namespace SolTechnology.Avro.Kafka.Deserialization;

/// <summary>
/// Deserializes messages in the Confluent Schema Registry wire format, written by AvroConvert or by the Confluent
/// serializers of any language, to plain .NET objects. The writer schema is fetched from the registry by the id in the
/// message and resolved against the schema of <typeparamref name="T"/> (Avro schema evolution).
/// </summary>
/// <remarks>
/// Use <c>.AsSyncOverAsync()</c> (or <c>SetAvroValueDeserializer</c>) with a consumer. A null message (tombstone)
/// is returned as <c>default</c>. Data contract rules and schema references are not supported.
/// </remarks>
public class AvroConvertSchemaRegistryDeserializer<T> : AsyncDeserializer<T, AvroSchema>
{
    private readonly AvroConvertOptions _options;
    private readonly AvroSchema _readerSchema;

    public AvroConvertSchemaRegistryDeserializer(ISchemaRegistryClient schemaRegistryClient, AvroConvertDeserializerConfig config = null)
        : base(schemaRegistryClient ?? throw new ArgumentNullException(nameof(schemaRegistryClient)), null)
    {
        config ??= new AvroConvertDeserializerConfig();
        _options = config.SerializerOptions;
        _readerSchema = AvroSchema.Create<T>(_options);

        schemaIdDecoder = config.SchemaIdStrategy.ToDeserializer();
        subjectNameStrategy = config.SubjectNameStrategy.ToAsyncDelegate(schemaRegistryClient, Enumerable.Empty<KeyValuePair<string, string>>(), out associatedNameStrategy);
    }

    public override async Task<T> DeserializeAsync(ReadOnlyMemory<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            return default;
        }

        var isKey = context.Component == MessageComponentType.Key;
        var subject = await GetSubjectName(context.Topic, isKey, null).ConfigureAwait(false);

        var schemaId = new SchemaId(SchemaType.Avro);
        var payload = schemaIdDecoder.Decode(data, context, ref schemaId);
        var (_, writerSchema) = await GetWriterSchema(subject, schemaId).ConfigureAwait(false);

        return AvroConvert.DeserializeHeadless<T>(payload.Span, writerSchema, _readerSchema, _options);
    }

    protected override Task<AvroSchema> ParseSchema(Schema schema)
    {
        if (schema.References is { Count: > 0 })
        {
            throw new NotSupportedException("AvroConvert does not support schema references yet.");
        }

        return Task.FromResult(AvroSchema.Parse(schema.SchemaString));
    }
}
