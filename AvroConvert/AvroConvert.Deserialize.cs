namespace AvroConvert
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using AutoMapper;


    public static partial class AvroConvert
    {
        public static List<Dictionary<string, object>> Deserialize(byte[] avroBytes)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

            var reader = Reader.Reader.OpenReader(new MemoryStream(avroBytes));

            List<dynamic> readResult = reader.GetEntries().ToList();

            result = readResult[0].contents;
            return result;
        }

        public static T Deserialize<T>(byte[] avroBytes)
        {
            var deserialized = Deserialize(avroBytes);

            Mapper.Initialize(cfg => { });
            T result = Mapper.Map<T>(deserialized);

            return result;
        }
    }
}
