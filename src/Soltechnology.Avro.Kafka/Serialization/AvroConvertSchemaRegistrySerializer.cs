using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;

namespace SolTechnology.Avro.Kafka.Serialization;

/// <summary>
/// Serializes plain .NET objects to Avro in the Confluent Schema Registry wire format, readable by the Confluent
/// serializers of any language. Subject naming, schema registration and the schema id encoding come from
/// <see cref="AsyncSerializer{T, TParsedSchema}"/> of Confluent.SchemaRegistry; AvroConvert writes the data.
/// </summary>
/// <remarks>
/// A null value is written as a null message (a tombstone on compacted topics).
/// Data contract rules and schema references are not supported.
/// </remarks>
public class AvroConvertSchemaRegistrySerializer<T> : AsyncSerializer<T, AvroSchema>
{
    private readonly AvroConvertOptions _options;
    private readonly AvroSchema _schema;
    private readonly string _recordName;
    private readonly Dictionary<string, SchemaId> _registeredSchemas = new();

    public AvroConvertSchemaRegistrySerializer(ISchemaRegistryClient schemaRegistryClient, AvroConvertSerializerConfig config = null)
        : base(schemaRegistryClient ?? throw new ArgumentNullException(nameof(schemaRegistryClient)), null)
    {
        config ??= new AvroConvertSerializerConfig();
        if (config.UseLatestVersion && config.AutoRegisterSchemas)
        {
            throw new ArgumentException("AvroConvertSchemaRegistrySerializer: UseLatestVersion cannot be combined with AutoRegisterSchemas.", nameof(config));
        }

        _options = config.SerializerOptions;
        _schema = AvroSchema.Create<T>(_options);
        _recordName = AvroSchemaNames.FullName(_schema);

        autoRegisterSchema = config.AutoRegisterSchemas;
        normalizeSchemas = config.NormalizeSchemas;
        useLatestVersion = config.UseLatestVersion;
        useLatestWithMetadata = config.UseLatestWithMetadata;
        useSchemaId = config.UseSchemaId ?? -1;
        schemaIdEncoder = config.SchemaIdStrategy.ToEncoder();
        subjectNameStrategy = config.SubjectNameStrategy.ToAsyncDelegate(schemaRegistryClient, Enumerable.Empty<KeyValuePair<string, string>>(), out associatedNameStrategy);
    }

    /// <summary>The schema generated from <typeparamref name="T"/> and registered by this serializer.</summary>
    public AvroSchema Schema => _schema;

    public override async Task<byte[]> SerializeAsync(T value, SerializationContext context)
    {
        if (value == null)
        {
            return null;
        }

        var isKey = context.Component == MessageComponentType.Key;
        var subject = await GetSubjectName(context.Topic, isKey, _recordName).ConfigureAwait(false);
        var writerSchema = _schema;
        SchemaId schemaId;

        await serdeMutex.WaitAsync().ConfigureAwait(false);
        try
        {
            var latest = await GetReaderSchema(subject).ConfigureAwait(false);
            if (latest != null)
            {
                schemaId = new SchemaId(SchemaType.Avro, latest.Id, latest.Guid);
                writerSchema = await GetParsedSchema(latest).ConfigureAwait(false);
            }
            else if (!_registeredSchemas.TryGetValue(subject, out schemaId))
            {
                var schema = new Schema(_schema.Json, new List<SchemaReference>(), SchemaType.Avro);
                var registered = autoRegisterSchema
                    ? await schemaRegistryClient.RegisterSchemaWithResponseAsync(subject, schema, normalizeSchemas).ConfigureAwait(false)
                    : await schemaRegistryClient.LookupSchemaAsync(subject, schema, true, normalizeSchemas).ConfigureAwait(false);
                schemaId = new SchemaId(SchemaType.Avro, registered.Id, registered.Guid);
                _registeredSchemas[subject] = schemaId;
            }
        }
        finally
        {
            serdeMutex.Release();
        }

        var data = new ArrayBufferWriter<byte>(initialBufferSize);
        AvroConvert.SerializeHeadless(value, data, writerSchema, _options);

        var prefixSize = schemaIdEncoder.CalculateSize(ref schemaId);
        var message = new byte[prefixSize + data.WrittenCount];
        schemaIdEncoder.Encode(message, ref context, ref schemaId);
        data.WrittenSpan.CopyTo(message.AsSpan(prefixSize));
        return message;
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
