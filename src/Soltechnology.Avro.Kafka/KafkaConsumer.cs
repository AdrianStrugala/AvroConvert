using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace SolTechnology.Avro.Kafka
{
    public class KafkaConsumer<TKey, TValue> : IDisposable
    {
        private readonly IConsumer<TKey, TValue> _consumer;
        private readonly Action<TKey, TValue, DateTime> _handler;
        private readonly CancellationToken _cancellationToken;
        private Task _taskConsumer;


        public KafkaConsumer(IConsumer<TKey, TValue> consumer, Action<TKey, TValue, DateTime> handler, CancellationToken cancellationToken)
        {
            _consumer = consumer;
            _handler = handler;
            _cancellationToken = cancellationToken;
        }


        public KafkaConsumer<TKey, TValue> StartConsuming()
        {
            _taskConsumer = StartConsumingInner();
            return this;
        }

        private async Task StartConsumingInner()
        {

            while (!_cancellationToken.IsCancellationRequested)
            {
                ConsumeResult<TKey, TValue> consumeResult = await Task.Run(() => _consumer.Consume(_cancellationToken), _cancellationToken);

                _handler(consumeResult.Message.Key, consumeResult.Message.Value, consumeResult.Message.Timestamp.UtcDateTime);
            }

            _consumer.Close();

        }


        public void Dispose()
        {
            _taskConsumer?.Wait(_cancellationToken);
        }
    }
}