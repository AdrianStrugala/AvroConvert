namespace AvroConvert
{
    using Newtonsoft.Json.Linq;

    public static partial class AvroConvert
    {
        public static JObject GetStructure(string schemaString)
        {
            //cut off avro specification substring
            schemaString = schemaString.Substring(schemaString.IndexOf("{"));
            
            return JObject.Parse(schemaString); ;
        }
    }
}
