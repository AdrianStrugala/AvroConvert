using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry;

namespace SolTechnology.Avro.Kafka.Serialization
{
    public class AvroConvertSchemaRegistrySerializer<T> : IAsyncSerializer<T>
    {
        private ISchemaRegistryClient RegistryClient { get; }
        private readonly ConcurrentDictionary<string, int> cache = new ConcurrentDictionary<string, int>();

        public AvroConvertSchemaRegistrySerializer(ISchemaRegistryClient registryClient)
        {
            RegistryClient = registryClient ?? throw new ArgumentNullException(nameof(registryClient));
        }

        public async Task<byte[]> SerializeAsync(T data, SerializationContext context)
        {
            string subject = $"{context.Topic }-{context.Component}";
            int id = 0;
            var schema = AvroConvert.GenerateSchema(typeof(T));

            if (cache.ContainsKey(subject))
            {
                id = cache[subject];
            }
            else
            {
                var existingSchema = await RegistryClient.GetLatestSchemaAsync(subject);

                if (existingSchema.SchemaString == schema)
                {
                    cache.AddOrUpdate(subject, existingSchema.Id, (key, oldValue) => existingSchema.Id);
                }
                else
                {
                    Confluent.SchemaRegistry.Schema confluentSchema = new Confluent.SchemaRegistry.Schema(
                        subject,
                        existingSchema.Version + 1,
                        0,
                        schema
                    );

                    id = await RegistryClient.RegisterSchemaAsync(subject, confluentSchema.ToString());
                    cache.AddOrUpdate(subject, id, (key, oldValue) => id);
                }
            }

            using (var stream = new MemoryStream())
            {
                //Confluent Kafka format:
                //https://docs.confluent.io/current/schema-registry/docs/serializer-formatter.html#wire-format

                //Magic number
                stream.WriteByte(0x00);

                //Id
                var idAsBytes = BitConverter.GetBytes(id);
                stream.Write(idAsBytes, 0, idAsBytes.Length);

                //Data
                var serializedData = AvroConvert.SerializeHeadless(data, schema);
                stream.Write(serializedData, 0, serializedData.Length);

                return stream.ToArray();
            }
        }
    }
}
