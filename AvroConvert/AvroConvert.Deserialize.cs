namespace AvroConvert
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;


    public static partial class AvroConvert
    {
        private const int MarkerLength = 16;
        private const int StopBytesLength = 1;

        public static List<Dictionary<string, object>> Deserialize(byte[] avroBytes)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

            var reader = Reader.Reader.OpenReader(new MemoryStream(avroBytes));

            var xd = reader.GetEntries().ToList();

            // result.Add(xd[0]);

            return result;
        }

        public static T Deserialize<T>(byte[] avroString)
        {
            T result = default(T);


            return result;
        }



        //        public static List<Dictionary<string, object>> Deserialize(byte[] avroString)
        //        {
        //            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();

        //            byte[] avroHeader = avroString.Take(4).ToArray();
        //            Contract.Assert(avroHeader[0] == 79);
        //            Contract.Assert(avroHeader[1] == 98);
        //            Contract.Assert(avroHeader[2] == 106);
        //            Contract.Assert(avroHeader[3] == 1);
        //
        //            int indexOfStopByte = Array.IndexOf(avroString, new byte());
        //            var schemaBytes = avroString.Take(indexOfStopByte).ToArray();
        //            var fileMarkerBytes = avroString.Skip(indexOfStopByte + StopBytesLength).Take(MarkerLength);
        //            var dataBytes = avroString.Skip(indexOfStopByte + StopBytesLength + MarkerLength).ToArray();
        //
        //            var structure = AvroConvert.GetSchema(System.Text.Encoding.UTF8.GetString(schemaBytes));
        //
        //            Stream stream = new MemoryStream(dataBytes);
        //
        //            var binaryDecoder = new BinaryDecoder(stream);
        //
        //
        //            var numberOfObjects = binaryDecoder.ReadLong();
        //            var sizeOfObjects = binaryDecoder.ReadLong();
        //
        //            var string1 = binaryDecoder.ReadString();
        //            var int1 = binaryDecoder.ReadInt();
        //            var string2 = binaryDecoder.ReadString();
        //var long1 = binaryDecoder.ReadLong();
        //
        //            var string3 = binaryDecoder.ReadString();
        //            var string4 = binaryDecoder.ReadString();
        //            var long2 = binaryDecoder.ReadLong();
        //
        //            return result;
        //        }
    }
}
