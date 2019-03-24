namespace AvroConvert
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static partial class AvroConvert
    {
        public static T Deserialize<T>(byte[] avroString)
        {
            T result = default(T);

            int indexOfStopByte = Array.IndexOf(avroString, new byte());
            var schemaBytes = avroString.Take(indexOfStopByte).ToArray();
            var dataBytes = avroString.Skip(indexOfStopByte).ToArray();

            var structure = AvroConvert.GetStructure(System.Text.Encoding.UTF8.GetString(schemaBytes));

            return result;
        }

        public static List<Dictionary<string, object>> Deserialize(byte[] avroString)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

            int indexOfStopByte = Array.IndexOf(avroString, new byte());
            var schemaBytes = avroString.Take(indexOfStopByte).ToArray();
            var dataBytes = avroString.Skip(indexOfStopByte).ToArray();


            var structure = AvroConvert.GetStructure(System.Text.Encoding.UTF8.GetString(schemaBytes));

            return result;
        }
    }
}
