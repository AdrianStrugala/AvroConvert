using System.IO;
using SolTechnology.Avro.Write;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        public static byte[] Serialize(object obj)
        {
            MemoryStream resultStream = new MemoryStream();

            string schema = GenerateSchema(obj.GetType(), true);
            using (var writer = new Encoder(Schema.Schema.Parse(schema), resultStream))
            {
                writer.Append(obj);
            }

            var result = resultStream.ToArray();
            return result;
        }
    }
}
