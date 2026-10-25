using System;
using AwesomeAssertions;
using Confluent.Kafka;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Kafka.Deserialization;
using SolTechnology.Avro.Kafka.Serialization;

namespace AvroConvertKafkaTests;

public class PlainAvroSerdeTests
{
    private static readonly SerializationContext Context = new(MessageComponentType.Value, "orders");

    [Test]
    public void Round_trip_without_a_registry()
    {
        var bytes = new AvroConvertSerializer<Order>().Serialize(Samples.Order(), Context);

        new AvroConvertDeserializer<Order>().Deserialize(bytes, false, Context).Should().BeEquivalentTo(Samples.Order());
    }

    [Test]
    public void Writes_plain_avro_without_a_schema_id()
    {
        var bytes = new AvroConvertSerializer<Order>().Serialize(Samples.Order(), Context);

        AvroConvert.DeserializeHeadless<Order>(bytes, AvroSchema.Create<Order>()).Should().BeEquivalentTo(Samples.Order());
    }

    [Test]
    public void Reads_data_written_with_another_schema()
    {
        var writerSchema = AvroSchema.Parse(Samples.JavaOrderSchema);
        var javaOrder = new JavaOrder { Id = 7, Customer = "Sol Technology", Total = 199.5, Paid = true, Tags = Samples.Order().Tags, Channel = "partner" };
        var bytes = new AvroConvertSerializer<JavaOrder>(writerSchema).Serialize(javaOrder, Context);

        new AvroConvertDeserializer<Order>(writerSchema).Deserialize(bytes, false, Context).Should().BeEquivalentTo(Samples.Order());
    }

    [Test]
    public void Null_is_a_tombstone_both_ways()
    {
        new AvroConvertSerializer<Order>().Serialize(null, Context).Should().BeNull();
        new AvroConvertDeserializer<Order>().Deserialize(ReadOnlySpan<byte>.Empty, true, Context).Should().BeNull();
    }
}
