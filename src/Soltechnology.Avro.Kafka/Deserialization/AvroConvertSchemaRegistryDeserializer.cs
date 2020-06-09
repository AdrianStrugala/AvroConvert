using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;

namespace SolTechnology.Avro.Kafka.Deserialization
{
    public class AvroConvertSchemaRegistryDeserializer<T> : IAsyncDeserializer<T>
    {
        private ISchemaRegistryClient RegistryClient { get; }
        private readonly ConcurrentDictionary<int, string> cache = new ConcurrentDictionary<int, string>();

        public AvroConvertSchemaRegistryDeserializer(ISchemaRegistryClient registryClient
        )
        {
            RegistryClient = registryClient ?? throw new ArgumentNullException(nameof(registryClient));
        }

        public virtual async Task<T> DeserializeAsync(ReadOnlyMemory<byte> data, bool isNull, SerializationContext context)
        {
            using (var stream = new MemoryStream(data.ToArray(), false))
            {
                //Confluent Kafka format:
                //https://docs.confluent.io/current/schema-registry/docs/serializer-formatter.html#wire-format
                if (stream.ReadByte() != 0x00)
                {
                    throw new InvalidDataException("Invalid Confluent Kafka data format");
                }
                var bytes = new byte[4];
                stream.Read(bytes, 0, bytes.Length);

                var id = BitConverter.ToInt32(bytes, 0);

                string schema;

                if (cache.ContainsKey(id))
                {
                    schema = cache[id];
                }
                else
                {
                    schema = await RegistryClient.GetSchemaAsync(id);
                    cache.AddOrUpdate(id, schema, (key, oldValue) => schema);
                }

                var confluentSchema = new ConfluentSchema(schema);
                var result = AvroConvert.DeserializeHeadless<T>(stream.ToArray(), confluentSchema.SchemaString);
                return result;
            }
        }
    }
}
