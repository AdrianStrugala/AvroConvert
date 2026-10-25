using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avro;
using Avro.Generic;
using AwesomeAssertions;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NUnit.Framework;
using SolTechnology.Avro.Kafka;
using SolTechnology.Avro.Kafka.Serialization;

namespace AvroConvertKafkaIntegrationTests;

public class Order
{
    public int Id { get; set; }
    public string Customer { get; set; }
    public double Total { get; set; }
    public List<string> Tags { get; set; }
}

/// <summary>Real broker and Schema Registry; the other side is the official Confluent Avro serde where it matters.</summary>
public class KafkaRoundTripTests
{
    private static string NewTopic() => $"orders-{Guid.NewGuid():N}";

    private static Order Sample(int id) => new() { Id = id, Customer = $"customer-{id}", Total = id * 1.5, Tags = new List<string> { "a", $"t{id}" } };

    private static IProducer<string, T> Producer<T>(Func<ProducerBuilder<string, T>, ProducerBuilder<string, T>> configure) =>
        configure(new ProducerBuilder<string, T>(new ProducerConfig { BootstrapServers = KafkaEnvironment.BootstrapServers })).Build();

    private static List<ConsumeResult<string, T>> Consume<T>(string topic, int count, Func<ConsumerBuilder<string, T>, ConsumerBuilder<string, T>> configure)
    {
        using var consumer = configure(new ConsumerBuilder<string, T>(new ConsumerConfig
        {
            BootstrapServers = KafkaEnvironment.BootstrapServers,
            GroupId = Guid.NewGuid().ToString(),
            AutoOffsetReset = AutoOffsetReset.Earliest
        })).Build();
        consumer.Subscribe(topic);

        var results = new List<ConsumeResult<string, T>>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (results.Count < count)
        {
            results.Add(consumer.Consume(timeout.Token));
        }

        consumer.Close();
        return results;
    }

    [Test]
    public async Task AvroConvert_producer_and_consumer_through_the_registry()
    {
        var topic = NewTopic();
        using var registry = KafkaEnvironment.CreateSchemaRegistryClient();

        using (var producer = Producer<Order>(b => b.SetAvroValueSerializer(registry)))
        {
            for (var i = 1; i <= 20; i++)
            {
                await producer.ProduceAsync(topic, new Message<string, Order> { Key = i.ToString(), Value = Sample(i) });
            }
        }

        var consumed = Consume<Order>(topic, 20, b => b.SetAvroValueDeserializer(registry));

        consumed.Select(r => r.Message.Value).Should().BeEquivalentTo(Enumerable.Range(1, 20).Select(Sample), o => o.WithStrictOrdering());
        (await registry.GetLatestSchemaAsync($"{topic}-value")).SchemaString.Should().Contain("\"Customer\"");
    }

    [Test]
    public async Task Confluent_consumer_reads_what_AvroConvert_produces()
    {
        var topic = NewTopic();
        using var registry = KafkaEnvironment.CreateSchemaRegistryClient();

        using (var producer = Producer<Order>(b => b.SetAvroValueSerializer(registry)))
        {
            await producer.ProduceAsync(topic, new Message<string, Order> { Key = "1", Value = Sample(1) });
        }

        var record = Consume<GenericRecord>(topic, 1, b => b.SetValueDeserializer(new AvroDeserializer<GenericRecord>(registry).AsSyncOverAsync())).Single().Message.Value;

        record["Id"].Should().Be(1);
        record["Customer"].Should().Be("customer-1");
        record["Total"].Should().Be(1.5);
    }

    [Test]
    public async Task AvroConvert_consumer_reads_what_the_Confluent_producer_writes()
    {
        var topic = NewTopic();
        using var registry = KafkaEnvironment.CreateSchemaRegistryClient();
        var schema = (RecordSchema)Avro.Schema.Parse("""
            {"type":"record","name":"Order","namespace":"com.example","fields":[
              {"name":"Id","type":"int"},{"name":"Customer","type":"string"},{"name":"Total","type":"double"},
              {"name":"Tags","type":{"type":"array","items":"string"}},{"name":"Channel","type":"string","default":"web"}]}
            """);
        var record = new GenericRecord(schema);
        record.Add("Id", 7);
        record.Add("Customer", "customer-7");
        record.Add("Total", 10.5);
        record.Add("Tags", new[] { "a", "t7" });
        record.Add("Channel", "partner");

        using (var producer = Producer<GenericRecord>(b => b.SetValueSerializer(new AvroSerializer<GenericRecord>(registry))))
        {
            await producer.ProduceAsync(topic, new Message<string, GenericRecord> { Key = "7", Value = record });
        }

        var order = Consume<Order>(topic, 1, b => b.SetAvroValueDeserializer(registry)).Single().Message.Value;

        order.Should().BeEquivalentTo(new Order { Id = 7, Customer = "customer-7", Total = 10.5, Tags = new List<string> { "a", "t7" } });
    }

    [Test]
    public async Task Null_value_is_a_tombstone()
    {
        var topic = NewTopic();
        using var registry = KafkaEnvironment.CreateSchemaRegistryClient();

        using (var producer = Producer<Order>(b => b.SetAvroValueSerializer(registry)))
        {
            await producer.ProduceAsync(topic, new Message<string, Order> { Key = "1", Value = Sample(1) });
            await producer.ProduceAsync(topic, new Message<string, Order> { Key = "1", Value = null });
        }

        var consumed = Consume<Order>(topic, 2, b => b.SetAvroValueDeserializer(registry));

        consumed[0].Message.Value.Id.Should().Be(1);
        consumed[1].Message.Value.Should().BeNull();
    }

    [Test]
    public async Task Writes_with_a_registered_schema_that_uses_references()
    {
        var topic = NewTopic();
        using var registry = KafkaEnvironment.CreateSchemaRegistryClient();
        var addressSubject = $"{topic}.Address";
        await registry.RegisterSchemaAsync(addressSubject, new Confluent.SchemaRegistry.Schema(
            """{"type":"record","name":"Address","namespace":"com.example.shared","fields":[{"name":"City","type":"string"}]}""",
            new List<SchemaReference>(), SchemaType.Avro));
        await registry.RegisterSchemaAsync($"{topic}-value", new Confluent.SchemaRegistry.Schema(
            """{"type":"record","name":"Shipment","namespace":"com.example","fields":[{"name":"Id","type":"int"},{"name":"To","type":"com.example.shared.Address"}]}""",
            new List<SchemaReference> { new("com.example.shared.Address", addressSubject, 1) }, SchemaType.Avro));

        using (var producer = Producer<Shipment>(b => b.SetAvroValueSerializer(registry, new AvroConvertSerializerConfig { AutoRegisterSchemas = false, UseLatestVersion = true })))
        {
            await producer.ProduceAsync(topic, new Message<string, Shipment> { Key = "1", Value = new Shipment { Id = 1, To = new Address { City = "Wrocław" } } });
        }

        Consume<Shipment>(topic, 1, b => b.SetAvroValueDeserializer(registry)).Single().Message.Value.To.City.Should().Be("Wrocław");
        var record = Consume<GenericRecord>(topic, 1, b => b.SetValueDeserializer(new AvroDeserializer<GenericRecord>(registry).AsSyncOverAsync())).Single().Message.Value;
        ((GenericRecord)record["To"])["City"].Should().Be("Wrocław");
    }

    public class Shipment
    {
        public int Id { get; set; }
        public Address To { get; set; }
    }

    public class Address
    {
        public string City { get; set; }
    }
}
