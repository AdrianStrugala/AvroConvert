namespace SolTechnology.Avro.Infrastructure.Extensions.JsonConvert
{
    internal static class JsonConvertExtensions
    {
        public static T DeserializeExpando<T>(string value)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value, new IntJsonConverter(), new ExpandoObjectJsonConverter());
        }
    }
}
