using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Extensions;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;
using SolTechnology.Avro.AvroObjectServices.Write;

namespace SolTechnology.Avro.Features.JsonToAvro
{
    [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
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
            using var output = new Writer();
            var schema = _jsonSchemaBuilder.BuildItemSchema(jArray);

            using (var writer = new Serialize.Encoder(schema, output, codecType))
            {
                foreach (var child in jArray)
                {
                    writer.Append(child);
                }
            }

            return output.ToArray();
        }
    }
}
