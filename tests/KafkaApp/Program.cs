using System;
using System.Collections.Generic;
using System.Threading;
using Avro.Generic;
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


            var kafkaConsumer = new KafkaConsumer(config,
                                                  // Callback to process consumed (key -> value) item
                                                  (key, value, utcTimestamp) =>
                                                  {
                                                      Console.Write($"C#     {key}  ->  ");
                                                      Console.Write($"{nameof(value.BatchId)} = {value.BatchId}   ");
                                                      Console.Write($"{nameof(value.Id)} = {value.Id}   ");
                                                      Console.Write($"{nameof(value.Name)} = {value.Name}   ");
                                                      Console.Write($"{nameof(value.NumericData)} = {value.NumericData}   ");
                                                      Console.Write($"{nameof(value.TextData)} = {value.TextData}   ");
                                                      Console.WriteLine($"   {utcTimestamp}");
                                                  },
                                                  // Callback to process log message
                                                  s => Console.WriteLine(s))
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
}

