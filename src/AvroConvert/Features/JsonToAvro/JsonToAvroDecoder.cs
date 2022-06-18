using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;
using SolTechnology.Avro.Infrastructure.Extensions;
using SolTechnology.Avro.Infrastructure.Extensions.JsonConverters;

namespace SolTechnology.Avro.Features.JsonToAvro
{
    internal class JsonToAvroDecoder
    {
        internal byte[] DecodeJson(string json, CodecType codecType)
        {
            object deserializedJson;

            //Array
            if (json.StartsWith("["))
            {
                deserializedJson = DecodeArray(json);
                return AvroConvert.Serialize(deserializedJson, codecType);
            }


            //Primitive
            deserializedJson = TryDecodePrimitive(json);
            if (deserializedJson != null)
            {
                return AvroConvert.Serialize(deserializedJson, codecType);
            }


            //Class
            var decoder = new ExpandoSerializer();
            var expando = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectJsonConverter());
            var result = decoder.SerializeExpando(expando, CodecType.Null);
            return result;
        }

        private object DecodeArray(string json)
        {
            var incomingObject = JsonConvert.DeserializeObject<object>(json);

            var enumerable = incomingObject.GetType().FindEnumerableType();
            if (enumerable != null)
            {
                var childItem = ((IList)incomingObject)[0];

                var xd = JsonConvert.DeserializeObject<List<ExpandoObject>>(json, new ExpandoObjectJsonConverter());
                // var result = decoder.SerializeExpando(xd, CodecType.Null);
                // return result;
                //dupa
            }

            return null;
        }

        private object TryDecodePrimitive(string json)
        {
            try
            {
                var incomingObject = JsonConvert.DeserializeObject<object>(json, new IntJsonConverter());
                var incomingObjectType = incomingObject?.GetType();

                if (incomingObjectType == typeof(string) ||
                    incomingObjectType == typeof(int) ||
                    incomingObjectType == typeof(decimal) ||
                    incomingObjectType == typeof(double) ||
                    incomingObjectType == typeof(float) ||
                    incomingObjectType == typeof(Guid) ||
                    incomingObjectType == typeof(Uri) ||
                    incomingObjectType == typeof(byte) ||
                    incomingObjectType == typeof(byte) ||
                    incomingObjectType == typeof(long))
                {
                    return incomingObject;
                }
            }
            catch (JsonSerializationException)
            {

            }


            return null;
        }
    }
}
