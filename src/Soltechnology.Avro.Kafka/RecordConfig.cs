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
                str =
                    "{\r\n\t\"subject\":\"aa-value\",\r\n            \"version\":3,\r\n            \"id\":1395,\r\n            \"schema\":\r\n\t{\r\n\t\t\"type\":\"record\",\r\n\t\t\"name\":\"aa\",\r\n\t\t\"namespace\":\"il.aa\",\r\n\t\t\"fields\":\r\n\t\t[\r\n\t\t\t{\"name\":\"Id\",           \"type\":\"int\"},\r\n\t\t\t{\"name\":\"Name\",         \"type\":\"string\"},\r\n\t\t\t{\"name\":\"BatchId\",      \"type\":[\"null\", \"int\"]},\r\n\t\t\t{\"name\":\"TextData\",     \"type\":[\"null\", \"string\"]},\r\n\t\t\t{\"name\":\"NumericData\",  \"type\":[\"null\", \"long\"], \"default\":null}\r\n\t\t]\r\n\t}\r\n}";

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
