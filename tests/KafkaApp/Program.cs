using System;
using System.Collections.Generic;
using System.Threading;
using Confluent.Kafka;
using Newtonsoft.Json;
using SolTechnology.Avro.Kafka;
using SolTechnology.Avro.Kafka.Deserialization;
using SolTechnology.Avro.Kafka.Serialization;

namespace KafkaApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var schema =
                "{\r\n\t\"subject\":\"aa-value\",\r\n            \"version\":3,\r\n            \"id\":1395,\r\n            \"schema\":\r\n\t{\r\n\t\t\"type\":\"record\",\r\n\t\t\"name\":\"aa\",\r\n\t\t\"namespace\":\"il.aa\",\r\n\t\t\"fields\":\r\n\t\t[\r\n\t\t\t{\"name\":\"Id\",           \"type\":\"int\"},\r\n\t\t\t{\"name\":\"Name\",         \"type\":\"string\"},\r\n\t\t\t{\"name\":\"BatchId\",      \"type\":[\"null\", \"int\"]},\r\n\t\t\t{\"name\":\"TextData\",     \"type\":[\"null\", \"string\"]},\r\n\t\t\t{\"name\":\"NumericData\",  \"type\":[\"null\", \"long\"], \"default\":null}\r\n\t\t]\r\n\t}\r\n}";

            const bool isFromLocalFile = true; //1

            const string schemaFileName = "schema.json";
            var urlSchemaPrefix = isFromLocalFile ? string.Empty : "http://localhost:9999/";

            var config = new Dictionary<string, object>
            {
                { KafkaPropNames.BootstrapServers, "localhost:9092" },
                { KafkaPropNames.SchemaRegistryUrl, $"{urlSchemaPrefix}{schemaFileName}" },
                { KafkaPropNames.Topic, "aa-topic" },
                { KafkaPropNames.GroupId, "aa-group" },
                { KafkaPropNames.Partition, 0 },
                { KafkaPropNames.Offset, 0 },
            };


            var consumer = new ConsumerBuilder<string, RecordModel>(new ConsumerConfig
            {
                BootstrapServers = (string)config[KafkaPropNames.BootstrapServers],
                GroupId = (string)config[KafkaPropNames.GroupId],
                AutoOffsetReset = AutoOffsetReset.Earliest
            }).SetKeyDeserializer(Deserializers.Utf8)
              .SetAvroValueDeserializer(schema)
              .Build();

            var topic = (string)config[KafkaPropNames.Topic];

            consumer.Assign(new List<TopicPartitionOffset>
            {
                new TopicPartitionOffset(topic, (int)config[KafkaPropNames.Partition], (int)config[KafkaPropNames.Offset])
            });


            var handler = new Handler();

            var kafkaConsumer = new KafkaConsumer<string, RecordModel>(
                    consumer,
                    (key, value, utcTimestamp) =>
                    {
                        Console.WriteLine($"C#     {key}  ->  ");
                        Console.WriteLine($"   {utcTimestamp}");
                        handler.Handle(value);
                        Console.WriteLine("");
                    }, CancellationToken.None)
                .StartConsuming();



            var producer = new ProducerBuilder<string, RecordModel>(
                    new ProducerConfig { BootstrapServers = (string)config[KafkaPropNames.BootstrapServers] })
                    .SetKeySerializer(Serializers.Utf8)
                    .SetValueSerializer(new AvroConvertSerializer<RecordModel>(schema))
                    .Build();


            var count = 0;
            var random = new Random(15);

            for (var i = 0; i < 10; i++)
            {
                count++;


                var record = new RecordModel
                {
                    Id = count,
                    Name = $"{config[KafkaPropNames.GroupId]}-{count}",
                    BatchId = (count / 10) + 1,
                    TextData = "Some text data",
                    NumericData = (long)random.Next(0, 100_000)
                };

                producer.Produce(topic, new Message<string, RecordModel>
                {
                    Key = count.ToString(),
                    Value = record
                });
            }

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();

            kafkaConsumer.Dispose();
        }
    }

    public class Handler
    {
        public void Handle(RecordModel recordModel)
        {
            Console.WriteLine(JsonConvert.SerializeObject(recordModel));
        }
    }
}

