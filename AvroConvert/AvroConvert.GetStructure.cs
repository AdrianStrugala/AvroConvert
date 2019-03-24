namespace AvroConvert
{
    using System.Linq;
    using Newtonsoft.Json.Linq;

    public static partial class AvroConvert
    {
        public static JObject GetStructure(string avroString)
        {
            JObject result = new JObject();

            avroString =  avroString.Substring(avroString.IndexOf("{"), avroString.LastIndexOf("}") + 1);
//            avroString.Substring(avroString.IndexOf("{"));
//            avroString.Substring(avroString.LastIndexOf("}"+1));

            var xd = avroString;

            result = JObject.Parse(avroString);

            return result;
        }
    }
}
