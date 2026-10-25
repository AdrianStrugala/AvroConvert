# SolTechnology.Avro.Kafka

`dotnet add package SolTechnology.Avro.Kafka --prerelease`

Serialization itself is described in [Documentation.md](Documentation.md); schemas, `AvroSchema` and compatibility checks in
[Documentation.Schema.md](Documentation.Schema.md).

`SolTechnology.Avro.Kafka` serializes plain .NET objects for Confluent Kafka in the
[Schema Registry wire format](https://docs.confluent.io/platform/current/schema-registry/fundamentals/serdes-develop/index.html#wire-format),
so the messages are read and written by the Confluent serializers of Java, Python, Go or .NET. Subject naming,
registration and the schema id encoding come from Confluent.SchemaRegistry; AvroConvert generates the schema of your
type and writes the data.

```csharp
using var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = "http://localhost:8081" });

using var producer = new ProducerBuilder<string, Order>(producerConfig)
    .SetAvroValueSerializer(schemaRegistry)          // registers the schema of Order under "{topic}-value"
    .Build();

using var consumer = new ConsumerBuilder<string, Order>(consumerConfig)
    .SetAvroValueDeserializer(schemaRegistry)        // writer schema from the registry, resolved against Order
    .Build();
```

| `AvroConvertSerializerConfig` | Default | |
|---|---|---|
| `AutoRegisterSchemas` | `true` | Register the schema of `T`; when `false` it must already be registered |
| `UseLatestVersion` / `UseLatestWithMetadata` / `UseSchemaId` | – | Write with a registered schema instead of the schema of `T` |
| `NormalizeSchemas` | `false` | Let the registry normalize the schema |
| `SubjectNameStrategy` | `Topic` | `Topic`, `Record`, `TopicRecord` |
| `SchemaIdStrategy` | `Prefix` | Schema id in front of the data (magic byte + 4 bytes) or in a message header |
| `SerializerOptions` | – | `AvroConvertOptions` (naming policy, converters…), also used to generate the schema |

`AvroConvertDeserializerConfig` has `SubjectNameStrategy`, `SchemaIdStrategy` (`Dual` by default: header if present,
else prefix) and `SerializerOptions`. Keys work the same way with `SetAvroKeySerializer` / `SetAvroKeyDeserializer`.
A null value is a null message (tombstone) in both directions. Schemas with
[references](https://docs.confluent.io/platform/current/schema-registry/fundamentals/serdes-develop/index.html#schema-references)
(types shared through their own subjects) are resolved from the registry when reading, and when writing with
`UseLatestVersion` / `UseSchemaId`. Data contract rules are not supported.

Without a schema registry, `AvroConvertSerializer<T>` / `AvroConvertDeserializer<T>` (or `SetAvroValueSerializer()`
without a client) write and read plain headless Avro, with the schema agreed out of band:

```csharp
.SetAvroValueSerializer(AvroSchema.Create<Order>())
.SetAvroValueDeserializer(writerSchema: AvroSchema.Parse(schemaJson))
```

A runnable example with Kafka and Schema Registry in Docker is in `samples/KafkaApp`.
