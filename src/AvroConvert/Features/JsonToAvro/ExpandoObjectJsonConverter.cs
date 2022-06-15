using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json;

namespace SolTechnology.Avro.Features.JsonToAvro
{
    public class ExpandoObjectJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ExpandoObject);
        }

        // only for deserialization
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var result = new ExpandoObject() as IDictionary<string, object>;
            reader.Read();

            while (reader.TokenType == JsonToken.PropertyName)
            {
                string propertyName = reader.Value as string;
                reader.Read();

                object value;
                if (reader.TokenType == JsonToken.Integer)
                {
                    value = Convert.ToInt64(reader.Value);

                    if ((long)value < int.MaxValue)
                    {
                        value = Convert.ToInt32(reader.Value); // safe convert to Int32 instead of Int64
                    }
                }
                else
                    value = serializer.Deserialize(reader);     // let the serializer handle all other cases
                result.Add(propertyName, value);
                reader.Read();
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
