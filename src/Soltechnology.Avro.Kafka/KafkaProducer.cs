using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avro;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;

namespace SolTechnology.Avro.Kafka
{
    public class KafkaProducer : IDisposable
    {
        #region Vars

        private Queue<KeyValuePair<string, GenericRecord>> _quePair = new Queue<KeyValuePair<string, GenericRecord>>();

        private IProducer<string, GenericRecord> _producer;
        private string _topic;

        private Action<string> _logger;

        public RecordSchema RecordSchema { get; }

        #endregion // Vars

        #region Ctor

        public KafkaProducer(Dictionary<string, object> config,
                             Action<string> logger)
        {
            if (logger == null)
                throw new Exception("Empty handler");

            _logger = logger;

            var genericRecordConfig = new RecordConfig((string)config[KafkaPropNames.SchemaRegistryUrl]);
            RecordSchema = genericRecordConfig.RecordSchema;

            _producer =
                new ProducerBuilder<string, GenericRecord>(new ProducerConfig { BootstrapServers = (string)config[KafkaPropNames.BootstrapServers] })
                    .SetKeySerializer(Serializers.Utf8)
                    .SetValueSerializer(new AvroSerializer<GenericRecord>(genericRecordConfig.GetSchemaRegistryClient()))
                    .Build();

            _topic = (string)config[KafkaPropNames.Topic];
        }

        #endregion // Ctor

        #region Send 

        public async Task SendAsync(string key, GenericRecord value)
        {

            var x = new RecordModel();
            x.Id = (int)value["Id"];
            x.Name = value["Name"].ToString();
            x.BatchId = (int)value["BatchId"];
            x.Name = value["Name"].ToString();
            x.TextData = "Some text data";
            x.NumericData = (long)value["NumericData"];

//            File.WriteAllBytes("y.avro", AvroConvert.SerializeHeadless(x, RecordSchema.ToString()));

            await _producer.ProduceAsync(_topic, new Message<string, GenericRecord>
            { Key = key, Value = value })
                           .ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                    _logger(task.Exception.Message);
                            });
        }

        #endregion // Send

        #region Dispose 

        public void Dispose()
        {
            _quePair = null;
            _producer?.Dispose();
            _logger("\nProducer was stopped.");
        }

        #endregion // Dispose 
    }
}

