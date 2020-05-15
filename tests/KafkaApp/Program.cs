using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avro;
using Avro.Generic;
using Confluent.Kafka;
using KafkaHelperLib;
using Newtonsoft.Json;
using SolTechnology.Avro.Kafka;

namespace KafkaApp
{
    class Program
    {
        static void Main(string[] args)
        {
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

            var genericRecordConfig = new RecordConfig((string)config[KafkaPropNames.SchemaRegistryUrl]);
            var RecordSchema = genericRecordConfig.RecordSchema;


            var consumer = new ConsumerBuilder<string, RecordModel>(new ConsumerConfig
            {
                BootstrapServers = (string)config[KafkaPropNames.BootstrapServers],
                GroupId = (string)config[KafkaPropNames.GroupId],
                AutoOffsetReset = AutoOffsetReset.Earliest
            }).SetKeyDeserializer(Deserializers.Utf8)
              .SetAvroValueDeserializer(RecordSchema.ToString())
              .Build();

            var topic = (string)config[KafkaPropNames.Topic];

            consumer.Assign(new List<TopicPartitionOffset>
            {
                new TopicPartitionOffset(topic, (int)config[KafkaPropNames.Partition], (int)config[KafkaPropNames.Offset])
            });


            var x = new Handler();

            var kafkaConsumer = new KafkaConsumer<string, RecordModel>(
                    consumer,
                    (key, value, utcTimestamp) =>
                    {
                        Console.WriteLine($"C#     {key}  ->  ");
                        Console.WriteLine($"   {utcTimestamp}");
                        x.Handle(value);
                        Console.WriteLine("");
                    }, CancellationToken.None)
                .StartConsuming();


            var kafkaProducer = new KafkaProducer(config,
                                                  // Callback to process log message
                                                  s => Console.WriteLine(s));

            var count = 0;
            var random = new Random(15);

            var timer = new Timer(_ =>
            {
                for (var i = 0; i < 10; i++)
                {
                    count++;


                    var gr = new GenericRecord(kafkaProducer.RecordSchema);
                    gr.Add("Id", count);
                    gr.Add("Name", $"{config[KafkaPropNames.GroupId]}-{count}");
                    gr.Add("BatchId", (count / 10) + 1);
                    gr.Add("TextData", "Some text data");
                    gr.Add("NumericData", (long)random.Next(0, 100_000));

                    kafkaProducer.SendAsync($"{count}", gr).Wait();
                }
            },
            null, 0, 5000);

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();

            timer.Dispose();
            kafkaProducer.Dispose();
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

