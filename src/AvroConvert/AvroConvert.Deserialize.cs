namespace EhwarSoft.Avro
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using AutoMapper;
    using Decoder;

    public static partial class AvroConvert
    {
        static AvroConvert()
        {
            Mapper.Initialize(cfg => { });
        }

        public static List<object> Deserialize(byte[] avroBytes)
        {
            var result = new List<object>();

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

            if (typeof(T).TryGetInterfaceGenericParameters(typeof(IEnumerable<>)))
            {
                result = Mapper.Map<T>(deserialized);
            }
            else
            {
                result = Mapper.Map<T>(deserialized[0]);
            }

            return result;
        }
    }
}
