using Newtonsoft.Json.Linq;
using SolTechnology.Avro.Features.JsonToAvro;

namespace SolTechnology.Avro
{
    public partial class SchemaConvert
    {
        /// <summary>
        /// Generates Avro schema for given JSON serialized object
        /// </summary>
        ///

        public static string GenerateFromJson(string json)
        {
            var schemaBuilder = new JsonSchemaBuilder();
            var token = JToken.Parse(json);
            var schema = schemaBuilder.BuildSchema(token);

            return schema.ToString();
        }
    }
}
