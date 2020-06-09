using System;
using System.Linq;
using Confluent.Kafka;

namespace SolTechnology.Avro.Kafka.Deserialization
{
    public class AvroConvertDeserializer<T> : IDeserializer<T>
    {
        private readonly string _schema;

        public AvroConvertDeserializer(string schema)
        {
            var confluentSchema = new ConfluentSchema(schema);
            _schema = confluentSchema.SchemaString;
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