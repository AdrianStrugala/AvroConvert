using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.Features.JsonToAvro
{
    internal class JsonToAvroDecoder
    {
        private readonly JsonSchemaBuilder _jsonSchemaBuilder;

        public JsonToAvroDecoder()
        {
            _jsonSchemaBuilder = new JsonSchemaBuilder();
        }

        internal byte[] DecodeJson(string json, CodecType codecType)
        {
            JsonReader jsonReader = new JsonTextReader(new StringReader(json));
            // jsonReader.FloatParseHandling = FloatParseHandling.Decimal;

            var token = JToken.Load(jsonReader);

            //Array
            if (token.Type == JTokenType.Array)
            {
                return SerializeJArray((JArray)token, codecType);
            }

            //Class
            if (token.Type == JTokenType.Object)
            {
                return SerializeJArray(new JArray(token), codecType);
            }

            //Assume Primitive
            return AvroConvert.Serialize(token.ToObject<object>(), codecType);

        }

        private byte[] SerializeJArray(JArray jArray, CodecType codecType)
        {
            using MemoryStream resultStream = new MemoryStream();
            var schema = _jsonSchemaBuilder.BuildSchema(jArray.FirstOrDefault());

            using (var writer = new Serialize.Encoder(schema, resultStream, codecType))
            {
                foreach (var child in jArray)
                {
                    writer.Append(child);
                }
            }

            var result = resultStream.ToArray();
            return result;
        }
    }
}
