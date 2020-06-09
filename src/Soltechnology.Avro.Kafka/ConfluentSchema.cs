using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SolTechnology.Avro.Kafka
{
    public class ConfluentSchema
    {
        public string SchemaString { get; }
        public string Subject { get; }
        public int Version { get; }
        public int Id { get; }

        public ConfluentSchema(string schema)
        {
            var json = JObject.Parse(schema.Replace(" ", string.Empty).Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("\\", ""));
            var dctProp = new Dictionary<string, object>();
            foreach (var property in json.Properties())
                dctProp[property.Name] = property.Value;


            SchemaString = dctProp.TryGetValue("schema", out object schemaOb) ? schemaOb.ToString() : null;
            Subject = dctProp.TryGetValue("subject", out object subjectOb) ? subjectOb.ToString() : null;
            Version = dctProp.TryGetValue("version", out object versionOb) ? (int)((dynamic)versionOb).Value : -1;
            Id = dctProp.TryGetValue("id", out object idOb) ? (int)((dynamic)idOb).Value : -1;
        }
    }
}
