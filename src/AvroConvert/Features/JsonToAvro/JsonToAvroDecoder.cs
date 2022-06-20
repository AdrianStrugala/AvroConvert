using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SolTechnology.Avro.Infrastructure.Extensions;
using SolTechnology.Avro.Infrastructure.Extensions.JsonConvert;

namespace SolTechnology.Avro.Features.JsonToAvro
{
    internal class JsonToAvroDecoder
    {
        private readonly ExpandoSerializer _expandoSerializer;

        public JsonToAvroDecoder()
        {
            _expandoSerializer = new ExpandoSerializer();
        }

        internal byte[] DecodeJson(string json, CodecType codecType)
        {
            object deserializedJson;

            //Array
            if (json.StartsWith("["))
            {
                var expandoList = DecodeArray(json);
                var sth = _expandoSerializer.SerializeExpando(expandoList, CodecType.Null);
                return sth;
            }


            //Primitive
            deserializedJson = TryDecodePrimitive(json);
            if (deserializedJson != null)
            {
                return AvroConvert.Serialize(deserializedJson, codecType);
            }


            //Class
            var expando = JsonConvertExtensions.DeserializeExpando<ExpandoObject>(json);
            var result = _expandoSerializer.SerializeExpando(expando, CodecType.Null);
            return result;
        }

        private List<ExpandoObject> DecodeArray(string json)
        {
            var incomingObject = JsonConvert.DeserializeObject<object>(json);

            var enumerable = incomingObject.GetType().FindEnumerableType();
            if (enumerable != null)
            {
                var childItem = ((IList)incomingObject)[0];

                var xd = JsonConvertExtensions.DeserializeExpando<List<ExpandoObject>>(json);
              
                return xd;
            }

            return null;
        }

        private object TryDecodePrimitive(string json)
        {
            try
            {
                var incomingObject = JsonConvertExtensions.DeserializeExpando<object>(json);
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
