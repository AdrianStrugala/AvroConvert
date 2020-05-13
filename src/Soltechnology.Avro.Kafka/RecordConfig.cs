using System;
using System.Collections.Generic;
using System.IO;
using Avro;
using Confluent.SchemaRegistry;
using Newtonsoft.Json.Linq;

namespace SolTechnology.Avro.Kafka
{
    public class RecordConfig
    {
        private string _schemaRegistryUrl;

        public string SchemaString { get; }
        public string Subject { get; }
        public int Version { get; }
        public int Id { get; }
        public RecordSchema RecordSchema { get; }

        public RecordConfig(string schemaRegistryUrl)
        {
            _schemaRegistryUrl = schemaRegistryUrl;

            var dctProp = GetSchemaString(schemaRegistryUrl);

            SchemaString = dctProp.TryGetValue("schema", out object schemaOb) ? schemaOb.ToString() : null;
            Subject = dctProp.TryGetValue("subject", out object subjectOb) ? subjectOb.ToString() : null;
            Version = dctProp.TryGetValue("version", out object versionOb) ? (int)((dynamic)versionOb).Value : -1;
            Id = dctProp.TryGetValue("id", out object idOb) ? (int)((dynamic)idOb).Value : -1;

            RecordSchema = (RecordSchema)RecordSchema.Parse(SchemaString);
        }

        private static IDictionary<string, object> GetSchemaString(string schemaRegistryUrl)
        {
            string str;

            try
            {
                str = "";

                //                str = Encoding.Default.GetString(new HttpClient().Get(schemaRegistryUrl, 100));
            }
            catch
            {
                try
                {
                    str = File.ReadAllText(schemaRegistryUrl);
                }
                catch
                {
                    Console.WriteLine($"ERROR: Schema string was not obtained from \"{schemaRegistryUrl}\".");
                    return null;
                }
            }

            if (string.IsNullOrEmpty(str))
                return null;

            try
            {
                var jOb = JObject.Parse(str.Replace(" ", string.Empty).Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("\\", ""));
                var dctProp = new Dictionary<string, object>();
                foreach (var property in jOb.Properties())
                    dctProp[property.Name] = property.Value;

                return dctProp;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public ISchemaRegistryClient GetSchemaRegistryClient(int schemaRegistryRequestTimeoutMs = 5000) =>
            //2
            //new CachedSchemaRegistryClient(new SchemaRegistryConfig
            //{
            //    SchemaRegistryUrl = _schemaRegistryUrl,
            //    SchemaRegistryRequestTimeoutMs = schemaRegistryRequestTimeoutMs,
            //});

            //2         
            new SchemaRegistryClient(new Confluent.SchemaRegistry.Schema(Subject, Version, Id, SchemaString));
    }
}
