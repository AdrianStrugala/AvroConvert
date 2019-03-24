namespace AvroConvert
{
    using System.Collections.Generic;

    public static partial class AvroConvert
    {
        public static T Deserialize<T>(byte[] avroString)
        {
            T result = default(T);

            AvroConvert.GetStructure(System.Text.Encoding.UTF8.GetString(avroString));

            return result;
        }

        public static List<Dictionary<string, object>> Deserialize(byte[] avroString)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

            AvroConvert.GetStructure(System.Text.Encoding.UTF8.GetString(avroString));

            return result;
        }
    }
}
