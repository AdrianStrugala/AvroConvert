using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.Features.JsonToAvro
{
    internal class JsonToAvroDecoder
    {
        private readonly JsonSerializer jsonSerializer;

        public JsonToAvroDecoder()
        {
            jsonSerializer = new JsonSerializer();
        }

        internal byte[] DecodeJson(string json, CodecType codecType)
        {
            var token = JToken.Parse(json);

            //Array
            if (token.Type == JTokenType.Array)
            {
                return SerializeJArray(DecodeArray(json), codecType);
            }


            //Class
            if (token.Type == JTokenType.Object)
            {
                var jObject = (JObject)token;
                return SerializeJArray(new List<JObject> { jObject }, codecType);
            }

            //Assume Primitive
            return AvroConvert.Serialize(token.ToObject<object>(), codecType);

        }

        private List<JObject> DecodeArray(string json)
        {
            var incomingObject = JsonConvert.DeserializeObject<object>(json);

            var enumerable = incomingObject.GetType().FindEnumerableType();
            if (enumerable != null)
            {
                var childItem = ((IList)incomingObject)[0];

                var xd = JsonConvert.DeserializeObject<List<JObject>>(json);

                return xd;
            }

            return null;
        }

        internal byte[] SerializeJArray(List<JObject> expandoObjects, CodecType codecType)
        {
            var expandoSchemaBuilder = new JsonSchemaBuilder();

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
