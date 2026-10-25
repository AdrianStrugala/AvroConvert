using System.Collections.Generic;
using System.Threading.Tasks;
using Avro;
using Avro.Generic;
using AwesomeAssertions;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using NUnit.Framework;
using SolTechnology.Avro.Kafka.Deserialization;
using SolTechnology.Avro.Kafka.Serialization;

namespace AvroConvertKafkaTests;

/// <summary>
/// The other side of each test is the official Confluent Avro serde (the one Java, Python and Go clients are compatible
/// with), so these tests pin down the wire format, not just a round trip through AvroConvert.
/// </summary>
public class ConfluentInteropTests
{
    private const string Topic = "orders";

    private static SerializationContext ValueContext() => new(MessageComponentType.Value, Topic, new Headers());

    [Test]
    public async Task Confluent_deserializer_reads_what_AvroConvert_writes()
    {
        var (client, _) = InMemorySchemaRegistry.Create();
        var bytes = await new AvroConvertSchemaRegistrySerializer<Order>(client).SerializeAsync(Samples.Order(), ValueContext());

        var record = await new AvroDeserializer<GenericRecord>(client, new AvroDeserializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic })
            .DeserializeAsync(bytes, false, ValueContext());

        record["Id"].Should().Be(7);
        record["Customer"].Should().Be("Sol Technology");
        record["Total"].Should().Be(199.5);
        record["Paid"].Should().Be(true);
        ((IEnumerable<object>)record["Tags"]).Should().Equal("licence", "business");
    }

    [Test]
    public async Task AvroConvert_reads_what_the_Confluent_serializer_writes_with_an_evolved_schema()
    {
        var (client, _) = InMemorySchemaRegistry.Create();
        var schema = (RecordSchema)Avro.Schema.Parse(Samples.JavaOrderSchema);
        var record = new GenericRecord(schema);
        record.Add("Id", 7);
        record.Add("Customer", "Sol Technology");
        record.Add("Total", 199.5);
        record.Add("Paid", true);
        record.Add("Tags", new[] { "licence", "business" });
        record.Add("Channel", "partner");

        var bytes = await new AvroSerializer<GenericRecord>(client, new AvroSerializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic })
            .SerializeAsync(record, ValueContext());
        var order = await new AvroConvertSchemaRegistryDeserializer<Order>(client).DeserializeAsync(bytes, false, ValueContext());

        order.Should().BeEquivalentTo(Samples.Order());
    }

    [Test]
    public async Task Schema_id_in_header_is_read_by_the_Confluent_deserializer()
    {
        var (client, _) = InMemorySchemaRegistry.Create();
        var context = ValueContext();
        var serializer = new AvroConvertSchemaRegistrySerializer<Order>(client, new AvroConvertSerializerConfig { SchemaIdStrategy = SchemaIdSerializerStrategy.Header });

        var bytes = await serializer.SerializeAsync(Samples.Order(), context);

        context.Headers.Should().ContainSingle(h => h.Key == "__value_schema_id");
        var record = await new AvroDeserializer<GenericRecord>(client, new AvroDeserializerConfig { SubjectNameStrategy = SubjectNameStrategy.Topic })
            .DeserializeAsync(bytes, false, context);
        record["Customer"].Should().Be("Sol Technology");
        (await new AvroConvertSchemaRegistryDeserializer<Order>(client).DeserializeAsync(bytes, false, context)).Should().BeEquivalentTo(Samples.Order());
    }
}
