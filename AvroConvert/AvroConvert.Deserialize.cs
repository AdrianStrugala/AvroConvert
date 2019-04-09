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

            var xd = reader.GetEntries().ToList();


            return result;
        }

        public static T Deserialize<T>(byte[] avroBytes)
        {
            T result = default(T);

            var reader = Reader.Reader.OpenReader(new MemoryStream(avroBytes));

            List<dynamic> readResult = reader.GetEntries().ToList();
            Mapper.Initialize(cfg => { });

            result = Mapper.Map<T>(readResult[0].contents);

            return result;
        }
    }
}
