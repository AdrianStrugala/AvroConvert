using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avro.Generic;
using AwesomeAssertions;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;
using SolTechnology.Avro.Kafka.Deserialization;
using SolTechnology.Avro.Kafka.Serialization;

namespace AvroConvertKafkaTests;

public class SchemaRegistrySerdeTests
{
    private const string Topic = "orders";
    private static readonly SerializationContext ValueContext = new(MessageComponentType.Value, Topic, new Headers());

    [Test]
    public async Task Round_trip_through_the_registry()
    {
        var (client, registry) = InMemorySchemaRegistry.Create();
        var serializer = new AvroConvertSchemaRegistrySerializer<Order>(client);
        var deserializer = new AvroConvertSchemaRegistryDeserializer<Order>(client);

        var bytes = await serializer.SerializeAsync(Samples.Order(), ValueContext);
        var result = await deserializer.DeserializeAsync(bytes, false, ValueContext);

        result.Should().BeEquivalentTo(Samples.Order());
        registry.Schemas.Should().ContainSingle().Which.Subject.Should().Be("orders-value");
    }

    [Test]
    public async Task Message_starts_with_magic_byte_and_big_endian_schema_id()
    {
        var (client, registry) = InMemorySchemaRegistry.Create();
        registry.Register("other-value", "\"string\"");
        var serializer = new AvroConvertSchemaRegistrySerializer<Order>(client);

        var bytes = await serializer.SerializeAsync(Samples.Order(), ValueContext);

        bytes[0].Should().Be(0);
        bytes.Skip(1).Take(4).Should().Equal(0, 0, 0, 2);
        AvroConvert.DeserializeHeadless<Order>(bytes.Skip(5).ToArray(), serializer.Schema).Should().BeEquivalentTo(Samples.Order());
    }

    [Test]
    public async Task Schema_is_registered_once_per_subject()
    {
        var (client, registry) = InMemorySchemaRegistry.Create();
        var serializer = new AvroConvertSchemaRegistrySerializer<Order>(client);

        for (var i = 0; i < 5; i++)
        {
            await serializer.SerializeAsync(Samples.Order(), ValueContext);
        }

        registry.RegisterCalls.Should().Be(1);
    }

    [Test]
    public async Task Without_auto_registration_an_unregistered_schema_fails()
    {
        var (client, _) = InMemorySchemaRegistry.Create();
        var serializer = new AvroConvertSchemaRegistrySerializer<Order>(client, new AvroConvertSerializerConfig { AutoRegisterSchemas = false });

        var act = () => serializer.SerializeAsync(Samples.Order(), ValueContext);

        await act.Should().ThrowAsync<SchemaRegistryException>();
    }

    [Test]
    public async Task Without_auto_registration_a_registered_schema_is_looked_up()
    {
        var (client, registry) = InMemorySchemaRegistry.Create();
        var schema = AvroSchema.Create<Order>();
        var registered = registry.Register("orders-value", schema.Json);
        var serializer = new AvroConvertSchemaRegistrySerializer<Order>(client, new AvroConvertSerializerConfig { AutoRegisterSchemas = false });

        var bytes = await serializer.SerializeAsync(Samples.Order(), ValueContext);

        bytes[4].Should().Be((byte)registered.Id);
    }

    [Test]
    public void Use_latest_version_and_auto_registration_cannot_be_combined()
    {
        var (client, _) = InMemorySchemaRegistry.Create();

        var act = () => new AvroConvertSchemaRegistrySerializer<Order>(client, new AvroConvertSerializerConfig { UseLatestVersion = true });

        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public async Task Record_name_strategy_uses_the_full_name_of_the_type()
    {
        var (client, registry) = InMemorySchemaRegistry.Create();
        var serializer = new AvroConvertSchemaRegistrySerializer<Order>(client, new AvroConvertSerializerConfig { SubjectNameStrategy = SubjectNameStrategy.Record });

        await serializer.SerializeAsync(Samples.Order(), ValueContext);

        registry.Schemas.Single().Subject.Should().Be("AvroConvertKafkaTests.Order");
    }

    [Test]
    public async Task Keys_use_the_key_subject()
    {
        var (client, registry) = InMemorySchemaRegistry.Create();
        var serializer = new AvroConvertSchemaRegistrySerializer<OrderKey>(client);
        var deserializer = new AvroConvertSchemaRegistryDeserializer<OrderKey>(client);
        var keyContext = new SerializationContext(MessageComponentType.Key, Topic);

        var bytes = await serializer.SerializeAsync(new OrderKey { Id = 42 }, keyContext);

        registry.Schemas.Single().Subject.Should().Be("orders-key");
        (await deserializer.DeserializeAsync(bytes, false, keyContext)).Id.Should().Be(42);
    }

    [Test]
    public async Task Null_is_a_tombstone_both_ways()
    {
        var (client, _) = InMemorySchemaRegistry.Create();

        (await new AvroConvertSchemaRegistrySerializer<Order>(client).SerializeAsync(null, ValueContext)).Should().BeNull();
        (await new AvroConvertSchemaRegistryDeserializer<Order>(client).DeserializeAsync(ReadOnlyMemory<byte>.Empty, true, ValueContext)).Should().BeNull();
    }

    [Test]
    public async Task Use_latest_version_writes_with_the_registered_schema()
    {
        var (client, registry) = InMemorySchemaRegistry.Create();
        registry.Register("orders-value", AvroSchema.Create<Order>().Json);
        var latest = registry.Register("orders-value", Samples.OrderSchemaV2);
        var serializer = new AvroConvertSchemaRegistrySerializer<Order>(client, new AvroConvertSerializerConfig { AutoRegisterSchemas = false, UseLatestVersion = true });

        var bytes = await serializer.SerializeAsync(Samples.Order(), ValueContext);

        bytes[4].Should().Be((byte)latest.Id);
        var record = await new AvroDeserializer<GenericRecord>(client, new AvroDeserializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic })
            .DeserializeAsync(bytes, false, ValueContext);
        record["Customer"].Should().Be("Sol Technology");
        record["Channel"].Should().BeNull();
    }

    [Test]
    public async Task Naming_policy_from_options_shapes_the_registered_schema()
    {
        var (client, registry) = InMemorySchemaRegistry.Create();
        var options = new AvroConvertOptions { NamingPolicy = new JavaAvroNamingPolicy("com.example.orders") };
        var serializer = new AvroConvertSchemaRegistrySerializer<Order>(client, new AvroConvertSerializerConfig { SerializerOptions = options });
        var deserializer = new AvroConvertSchemaRegistryDeserializer<Order>(client, new AvroConvertDeserializerConfig { SerializerOptions = options });

        var bytes = await serializer.SerializeAsync(Samples.Order(), ValueContext);

        registry.Schemas.Single().SchemaString.Should().Contain("com.example.orders");
        (await deserializer.DeserializeAsync(bytes, false, ValueContext)).Should().BeEquivalentTo(Samples.Order());
    }
}
