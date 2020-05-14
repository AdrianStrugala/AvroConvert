using System;
using System.Linq;
using Confluent.Kafka;
using SolTechnology.Avro;

namespace KafkaHelperLib
{
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