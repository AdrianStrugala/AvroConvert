# SolTechnology.Avro.Kafka

Confluent Kafka serializers for plain C# classes, built on [AvroConvert](https://www.nuget.org/packages/AvroConvert). There is no code generation and no `ISpecificRecord`. Messages use the Confluent Schema Registry wire format, so the Confluent serializers of Java, Python, Go and .NET read and write them.

```
dotnet add package SolTechnology.Avro.Kafka
```

## With Schema Registry

```csharp
using var schemaRegistry = new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = "http://localhost:8081" });

using var producer = new ProducerBuilder<string, Order>(producerConfig)
    .SetAvroValueSerializer(schemaRegistry)          // registers the schema of Order under "{topic}-value"
    .Build();

using var consumer = new ConsumerBuilder<string, Order>(consumerConfig)
    .SetAvroValueDeserializer(schemaRegistry)        // writer schema from the registry, resolved against Order
    .Build();
```

- **Registry handling:** subject name strategies (`Topic`, `Record`, `TopicRecord`), auto-registration, use latest version / schema id, and the schema id in the prefix or in a message header. All of this comes from Confluent.SchemaRegistry.
- **Schemas:** schema evolution, and schema references (types shared through their own subjects).
- **Messages:** tombstones (null values) in both directions, and keys through `SetAvroKeySerializer` / `SetAvroKeyDeserializer`.
- **Options:** `AvroConvertOptions` (naming policy, converters) through `AvroConvertSerializerConfig` / `AvroConvertDeserializerConfig`.

## Without a registry

Plain headless Avro, with the schema agreed out of band:

```csharp
.SetAvroValueSerializer(AvroSchema.Create<Order>())
.SetAvroValueDeserializer(writerSchema: AvroSchema.Parse(schemaJson))
```

## Links

- [Documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#kafka)
- [Sample with Kafka and Schema Registry in Docker](https://github.com/AdrianStrugala/AvroConvert/tree/master/samples/KafkaApp)
- [Kafka message decoder](https://soltechnology.dev/avroconvert/online/kafka-message-decoder/): magic byte, schema id and payload in your browser

## Licence

Free for noncommercial use and for companies under 100 people and USD 1M revenue (PolyForm Noncommercial / Small Business). Other organisations need a commercial licence: [soltechnology.dev/avroconvert](https://soltechnology.dev/avroconvert/). See LICENSE.md in the package.
