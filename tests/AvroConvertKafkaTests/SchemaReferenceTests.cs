using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avro.Generic;
using AwesomeAssertions;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Kafka.Deserialization;
using SolTechnology.Avro.Kafka.Serialization;

namespace AvroConvertKafkaTests;

/// <summary>
/// Schema Registry references: the Order schema uses Address and Status, which are registered under their own subjects,
/// as Java producers with shared types do.
/// </summary>
public class SchemaReferenceTests
{
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    public enum Status
    {
        New,
        Paid
    }

    public class ShippedOrder
    {
        public int Id { get; set; }
        public Address ShipTo { get; set; }
        public Status Status { get; set; }
    }

    private const string AddressSchema = """{"type":"record","name":"Address","namespace":"com.example.shared","fields":[{"name":"Street","type":"string"},{"name":"City","type":"string"}]}""";
    private const string StatusSchema = """{"type":"enum","name":"Status","namespace":"com.example.shared","symbols":["New","Paid"]}""";
    private const string OrderSchema = """{"type":"record","name":"ShippedOrder","namespace":"com.example.orders","fields":[{"name":"Id","type":"int"},{"name":"ShipTo","type":"com.example.shared.Address"},{"name":"Status","type":"com.example.shared.Status"}]}""";

    private static readonly ShippedOrder Sample = new() { Id = 5, ShipTo = new Address { Street = "Młodych Techników 4", City = "Wrocław" }, Status = Status.Paid };

    private static SerializationContext Context() => new(MessageComponentType.Value, "orders", new Headers());

    private static (ISchemaRegistryClient Client, RegisteredSchema Order) RegistryWithReferences()
    {
        var (client, registry) = InMemorySchemaRegistry.Create();
        registry.Register("com.example.shared.Address", AddressSchema);
        registry.Register("com.example.shared.Status", StatusSchema);
        var order = registry.Register("orders-value", OrderSchema, new List<SchemaReference>
        {
            new("com.example.shared.Address", "com.example.shared.Address", 1),
            new("com.example.shared.Status", "com.example.shared.Status", 1)
        });
        return (client, order);
    }

    /// <summary>Confluent wire format: magic byte 0, big-endian schema id, Avro binary.</summary>
    private static byte[] Frame(int schemaId, byte[] payload) =>
        new byte[] { 0 }.Concat(System.BitConverter.GetBytes(IPAddress.HostToNetworkOrder(schemaId))).Concat(payload).ToArray();

    [Test]
    public async Task Deserializer_resolves_referenced_types_from_the_registry()
    {
        var (client, order) = RegistryWithReferences();
        var full = AvroSchema.Parse(OrderSchema, new[] { StatusSchema, AddressSchema });
        var message = Frame(order.Id, AvroConvert.SerializeHeadless(Sample, full));

        var result = await new AvroConvertSchemaRegistryDeserializer<ShippedOrder>(client).DeserializeAsync(message, false, Context());

        result.Should().BeEquivalentTo(Sample);
    }

    [Test]
    public async Task Confluent_deserializer_agrees_on_the_bytes()
    {
        var (client, order) = RegistryWithReferences();
        var full = AvroSchema.Parse(OrderSchema, new[] { AddressSchema, StatusSchema });
        var message = Frame(order.Id, AvroConvert.SerializeHeadless(Sample, full));

        var record = await new AvroDeserializer<GenericRecord>(client, new AvroDeserializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic })
            .DeserializeAsync(message, false, Context());

        ((GenericRecord)record["ShipTo"])["City"].Should().Be("Wrocław");
        ((GenericEnum)record["Status"]).Value.Should().Be("Paid");
    }

    [Test]
    public async Task Serializer_writes_with_a_latest_schema_that_has_references()
    {
        var (client, order) = RegistryWithReferences();
        var serializer = new AvroConvertSchemaRegistrySerializer<ShippedOrder>(client, new AvroConvertSerializerConfig { AutoRegisterSchemas = false, UseLatestVersion = true });

        var message = await serializer.SerializeAsync(Sample, Context());

        message[4].Should().Be((byte)order.Id);
        (await new AvroConvertSchemaRegistryDeserializer<ShippedOrder>(client).DeserializeAsync(message, false, Context())).Should().BeEquivalentTo(Sample);
        var record = await new AvroDeserializer<GenericRecord>(client, new AvroDeserializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic })
            .DeserializeAsync(message, false, Context());
        ((GenericRecord)record["ShipTo"])["Street"].Should().Be("Młodych Techników 4");
    }
}
