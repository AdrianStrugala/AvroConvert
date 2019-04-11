namespace AvroConvert
{
    using AutoMapper;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Decoder;


    public static partial class AvroConvert
    {
        public static List<Dictionary<string, object>> Deserialize(byte[] avroBytes)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

            var reader = Reader.OpenReader(new MemoryStream(avroBytes));

            List<dynamic> readResult = reader.GetEntries().ToList();

            foreach (var read in readResult)
            {
                result.Add(read);
            }

            return result;
        }

        public static T Deserialize<T>(byte[] avroBytes)
        {
            T result;
            var deserialized = Deserialize(avroBytes);

            Mapper.Initialize(cfg => { });

            if (typeof(T).TryGetInterfaceGenericParameters(typeof(IEnumerable<>)))
            {
                result = Mapper.Map<T>(deserialized);
            }
            else
            {
                result = Mapper.Map<T>(deserialized[0]);
            }

            Mapper.Reset();

            return result;
        }
    }
}
