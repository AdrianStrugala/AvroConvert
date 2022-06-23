using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace SolTechnology.Avro.Features.JsonToAvro
{
    internal class ExpandoSerializer
    {
        internal byte[] SerializeExpando(JObject expandoObject, CodecType codecType)
        {
            var expandoSchemaBuilder = new ExpandoSchemaBuilder();

            using (MemoryStream resultStream = new MemoryStream())
            {
                var schema = expandoSchemaBuilder.BuildSchema(expandoObject);

                using (var writer = new Serialize.Encoder(schema, resultStream, codecType))
                {
                    writer.Append(expandoObject);
                }

                var result = resultStream.ToArray();
                return result;
            }
        }

        internal byte[] SerializeExpando(List<JObject> expandoObjects, CodecType codecType)
        {
            var expandoSchemaBuilder = new ExpandoSchemaBuilder();

            using (MemoryStream resultStream = new MemoryStream())
            {
                var schema = expandoSchemaBuilder.BuildSchema(expandoObjects.FirstOrDefault());

                using (var writer = new Serialize.Encoder(schema, resultStream, codecType))
                {
                    foreach (var expandoObject in expandoObjects)
                    {

                        writer.Append(expandoObject);
                    }
                }

                var result = resultStream.ToArray();
                return result;
            }
        }
    }
}
