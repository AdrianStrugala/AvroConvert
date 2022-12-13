using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
            // string subject = $"{context.Topic}-{context.Component}";
            int id = 0;
            var schema = AvroConvert.GenerateSchema(typeof(T));
            var dataTypeName = typeof(T).FullName;
            string subject = context.Component == MessageComponentType.Key ? RegistryClient.ConstructKeySubjectName(context.Topic, dataTypeName) : RegistryClient.ConstructValueSubjectName(context.Topic, dataTypeName);

            if (cache.ContainsKey(subject))
            {
                id = cache[subject];
            }
            else
            {
                id = 0;

                var existingSchema = await RegistryClient.GetLatestSchemaAsync(subject);

                if (existingSchema.SchemaString == schema)
                {
                    cache.AddOrUpdate(subject, existingSchema.Id, (oldKey, oldValue) => existingSchema.Id);
                }
                else
                {
                    var confluentSchema = new RegisteredSchema(
                        subject,
                        existingSchema.Version + 1,
                        existingSchema.Id,
                        schema,
                        SchemaType.Avro,
                        new List<SchemaReference>()
                    );

                    id = await RegistryClient.RegisterSchemaAsync(subject, confluentSchema);
                    cache.AddOrUpdate(subject, id, (oldKey, oldValue) => id);
                }
            }

            using (var stream = new MemoryStream())
            {
                //Confluent Kafka format:
                //https://docs.confluent.io/current/schema-registry/docs/serializer-formatter.html#wire-format

                //Magic number
                stream.WriteByte(0x00);

                //Id
                id = IPAddress.HostToNetworkOrder(id); //ensure correct order
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
