namespace AvroConvert
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using AutoMapper;
    using Read;

    public static partial class AvroConvert
    {
        static AvroConvert()
        {
            Mapper.Initialize(cfg => { });
        }

        public static List<object> Deserialize(byte[] avroBytes, Schema.Schema schema = null)
        {
            var result = new List<object>();

            var reader = Decoder.OpenReader(new MemoryStream(avroBytes), schema);

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

            string schema = AvroConvert.GenerateSchema(typeof(T), true);
            var deserialized = Deserialize(avroBytes, Schema.Schema.Parse(schema));

            result = Mapper.Map<T>(deserialized[0]);

            return result;
        }
    }
}
