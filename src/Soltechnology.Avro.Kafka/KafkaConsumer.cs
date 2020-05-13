using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avro;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using SolTechnology.Avro;

namespace KafkaHelperLib
{
    public class KafkaConsumer : IDisposable
    {
        #region Vars

        private IConsumer<string, RecordModel> _consumer;
        private CancellationTokenSource _cts;
        private Action<string, RecordModel, DateTime> _consumeResultHandler;
        private Action<string> _logger;
        private string _topic;
        private Task _taskConsumer;

        public RecordConfig GenericRecordConfig { get; }
        public RecordSchema RecordSchema { get; }

        #endregion // Vars

        #region Ctor

        public KafkaConsumer(Dictionary<string, object> config, Action<string, RecordModel, DateTime> consumeResultHandler, Action<string> logger)
        {
            if (consumeResultHandler == null || logger == null)
                throw new Exception("Empty handler");

            _consumeResultHandler = consumeResultHandler;
            _logger = logger;

            _cts = new CancellationTokenSource();

            var genericRecordConfig = new RecordConfig((string)config[KafkaPropNames.SchemaRegistryUrl]);
            RecordSchema = genericRecordConfig.RecordSchema;


            _consumer = new ConsumerBuilder<string, RecordModel>(new ConsumerConfig
            {
                BootstrapServers = (string)config[KafkaPropNames.BootstrapServers],
                GroupId = (string)config[KafkaPropNames.GroupId],
                AutoOffsetReset = AutoOffsetReset.Earliest
            }).SetKeyDeserializer(Deserializers.Utf8)
              .SetAvroValueDeserializer(RecordSchema.ToString())
              .SetErrorHandler((_, e) => logger(e.Reason)).Build();

            _topic = (string)config[KafkaPropNames.Topic];

            _consumer.Assign(new List<TopicPartitionOffset>
            {
                new TopicPartitionOffset(_topic, (int)config[KafkaPropNames.Partition], (int)config[KafkaPropNames.Offset])
            });
        }

        #endregion // Ctor

        #region StartConsuming Method

        public KafkaConsumer StartConsuming()
        {
            _taskConsumer = StartConsumingInner();
            return this;
        }

        private async Task StartConsumingInner()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var cr = await Task<ConsumeResult<string, byte[]>>.Run(() =>
                                                                           {
                                                                               try
                                                                               {
                                                                                   return _consumer.Consume(_cts.Token);
                                                                               }
                                                                               catch (Exception e)
                                                                               {
                                                                                   _logger(e.Message);
                                                                               }

                                                                               return null;
                                                                           });

                    if (cr != null)
                        _consumeResultHandler(cr.Key, cr.Value, cr.Timestamp.UtcDateTime);
                }
            }
            catch (Exception e)
            {
                _logger(e.Message);
            }
            finally
            {
                _consumer.Close();
            }
        }

        #endregion // StartConsuming Method

        #region Dispose 

        public void Dispose()
        {
            _cts?.Cancel();
            _logger("\nConsumer was stopped.");
            _taskConsumer?.Wait();
        }

        #endregion // Dispose
    }

    public static class ConsumerBuilderExtensions
    {
        public static ConsumerBuilder<TKey, TValue> SetAvroValueDeserializer<TKey, TValue>(this ConsumerBuilder<TKey, TValue> consumerBuilder, string schema)
        {
            var des = new AvroConvertDeserializer<TValue>(schema);
            return consumerBuilder.SetValueDeserializer(des);
        }

        public class AvroConvertDeserializer<T> : IDeserializer<T>
        {
            private string _schema;

            public AvroConvertDeserializer(string schema)
            {
                _schema = schema;
            }

            public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
            {
                var dataArray = data.ToArray();
                var dataWithoutMagicNumber = dataArray.Skip(5);

                var result = AvroConvert.DeserializeHeadless<T>(dataWithoutMagicNumber.ToArray(), _schema);
                return result;
            }
        }
    }
}