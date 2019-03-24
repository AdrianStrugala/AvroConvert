namespace AvroConvert
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Avro.IO;

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
            var dataBytes = avroString.Skip(indexOfStopByte+1).ToArray();


            var structure = AvroConvert.GetStructure(System.Text.Encoding.UTF8.GetString(schemaBytes));

            Stream stream = new MemoryStream(dataBytes);

            var binaryDecoder = new BinaryDecoder(stream);
        //    var string1 = binaryDecoder.ReadString();
         //   var string2 = binaryDecoder.ReadString();
         //   var long1 = binaryDecoder.ReadLong();

         var sth = ReadString(dataBytes);

            return result;
        }

        public static string ReadString(byte[] data)
        {
            int length = data[0];
            byte[] buffer = new byte[length];
            buffer = data.Take(length).ToArray();
            return System.Text.Encoding.UTF8.GetString(buffer);
        }
    }
}
