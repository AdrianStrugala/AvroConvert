namespace AvroConvert
{
    using System.IO;
    using Write;

    public static partial class AvroConvert
    {
        public static byte[] Serialize(object obj)
        {
            MemoryStream resultStream = new MemoryStream();

            string schema = AvroConvert.GenerateSchema(obj.GetType());
            using (var writer = new Encoder(Schema.Schema.Parse(schema), resultStream))
            {
                writer.Append(obj);
            }

            var result = resultStream.ToArray();
            return result;
        }
    }
}
