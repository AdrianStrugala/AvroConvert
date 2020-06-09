using System.IO;
using Confluent.Kafka;

namespace SolTechnology.Avro.Kafka.Serialization
{
    public class AvroConvertSerializer<T> : ISerializer<T>
    {
        private readonly string _schema;

        public AvroConvertSerializer(string schema)
        {
            var confluentSchema = new ConfluentSchema(schema);
            _schema = confluentSchema.SchemaString;
        }

        public byte[] Serialize(T data, SerializationContext context)
        {
            using (var stream = new MemoryStream())
            {
                //Magic number
                stream.WriteByte(0x00);

                //Id is unknown and not needed - put 0000
                stream.WriteByte(0x00);
                stream.WriteByte(0x00);
                stream.WriteByte(0x00);
                stream.WriteByte(0x00);

                var serializedData = AvroConvert.SerializeHeadless(data, _schema);
                stream.Write(serializedData, 0, serializedData.Length);

                return stream.ToArray();
            }
        }
    }
}
