using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using SolTechnology.Avro.Kafka.Deserialization;
using SolTechnology.Avro.Kafka.Serialization;

namespace SolTechnology.Avro.Kafka;

public static class KafkaBuilderExtensions
{
    /// <summary>Serializes values with AvroConvert in the Confluent Schema Registry wire format.</summary>
    public static ProducerBuilder<TKey, TValue> SetAvroValueSerializer<TKey, TValue>(this ProducerBuilder<TKey, TValue> builder, ISchemaRegistryClient schemaRegistryClient, AvroConvertSerializerConfig config = null) =>
        builder.SetValueSerializer(new AvroConvertSchemaRegistrySerializer<TValue>(schemaRegistryClient, config));

    /// <summary>Serializes keys with AvroConvert in the Confluent Schema Registry wire format.</summary>
    public static ProducerBuilder<TKey, TValue> SetAvroKeySerializer<TKey, TValue>(this ProducerBuilder<TKey, TValue> builder, ISchemaRegistryClient schemaRegistryClient, AvroConvertSerializerConfig config = null) =>
        builder.SetKeySerializer(new AvroConvertSchemaRegistrySerializer<TKey>(schemaRegistryClient, config));

    /// <summary>Serializes values to plain Avro binary without a schema registry.</summary>
    public static ProducerBuilder<TKey, TValue> SetAvroValueSerializer<TKey, TValue>(this ProducerBuilder<TKey, TValue> builder, AvroSchema schema = null, AvroConvertOptions options = null) =>
        builder.SetValueSerializer(new AvroConvertSerializer<TValue>(schema, options));

    /// <summary>Deserializes values in the Confluent Schema Registry wire format with AvroConvert.</summary>
    public static ConsumerBuilder<TKey, TValue> SetAvroValueDeserializer<TKey, TValue>(this ConsumerBuilder<TKey, TValue> builder, ISchemaRegistryClient schemaRegistryClient, AvroConvertDeserializerConfig config = null) =>
        builder.SetValueDeserializer(new AvroConvertSchemaRegistryDeserializer<TValue>(schemaRegistryClient, config).AsSyncOverAsync());

    /// <summary>Deserializes keys in the Confluent Schema Registry wire format with AvroConvert.</summary>
    public static ConsumerBuilder<TKey, TValue> SetAvroKeyDeserializer<TKey, TValue>(this ConsumerBuilder<TKey, TValue> builder, ISchemaRegistryClient schemaRegistryClient, AvroConvertDeserializerConfig config = null) =>
        builder.SetKeyDeserializer(new AvroConvertSchemaRegistryDeserializer<TKey>(schemaRegistryClient, config).AsSyncOverAsync());

    /// <summary>Deserializes values from plain Avro binary without a schema registry.</summary>
    public static ConsumerBuilder<TKey, TValue> SetAvroValueDeserializer<TKey, TValue>(this ConsumerBuilder<TKey, TValue> builder, AvroSchema writerSchema = null, AvroConvertOptions options = null) =>
        builder.SetValueDeserializer(new AvroConvertDeserializer<TValue>(writerSchema, options));
}
