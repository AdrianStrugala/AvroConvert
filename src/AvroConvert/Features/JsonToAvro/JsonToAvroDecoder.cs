using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
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

        internal byte[] DecodeJson(string json, AvroConvertOptions options)
        {
            JsonReader jsonReader = new JsonTextReader(new StringReader(json));
            // jsonReader.FloatParseHandling = FloatParseHandling.Decimal;

            var token = JToken.Load(jsonReader);

            //Array
            if (token.Type == JTokenType.Array)
            {
                return SerializeJArray((JArray)token, options);
            }

            //Class
            if (token.Type == JTokenType.Object)
            {
                return SerializeJArray(new JArray(token), options);
            }

            //Assume Primitive
            return options == null ? AvroConvert.Serialize(token.ToObject<object>()) : AvroConvert.Serialize(token.ToObject<object>(), options);

        }

        private byte[] SerializeJArray(JArray jArray, AvroConvertOptions options)
        {
            using var output = new Writer();
            var schema = RuntimeSchema.Intern(_jsonSchemaBuilder.BuildItemSchema(jArray), options);

            using (var writer = new Serialize.Encoder(schema, output, options?.Codec ?? CodecType.Null, options))
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
