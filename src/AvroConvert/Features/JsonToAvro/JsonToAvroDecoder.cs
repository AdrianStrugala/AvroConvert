using System.Dynamic;
using System.IO;

namespace SolTechnology.Avro.Features.JsonToAvro
{
    internal class JsonToAvroDecoder
    {
        internal byte[] SerializeExpando(ExpandoObject expandoObject, CodecType codecType)
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
    }
}
