using Newtonsoft.Json;
using System;

namespace SolTechnology.Avro.Infrastructure.Extensions.JsonConverters
{
    internal class IntJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(long) || objectType == typeof(long?) || objectType == typeof(object);
        }



        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value is long longValue)
            {
                if (longValue >= int.MinValue && longValue <= int.MaxValue)
                {
                    return Convert.ToInt32(reader.Value);
                }
            }
            return reader.Value;
        }

        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
