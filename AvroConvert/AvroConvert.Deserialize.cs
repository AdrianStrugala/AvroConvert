namespace AvroConvert
{
    using AutoMapper;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;


    public static partial class AvroConvert
    {
        public static Dictionary<string, object> Deserialize(byte[] avroBytes)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            var reader = Reader.Reader.OpenReader(new MemoryStream(avroBytes));

            List<dynamic> readResult = reader.GetEntries().ToList();

            result = readResult[0];
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
