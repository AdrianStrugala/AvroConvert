namespace AvroConvert
{
    using Avro;
    using Encoder;
    using Microsoft.Hadoop.Avro;
    using System.IO;
    using System.Runtime.Serialization;

    public static partial class AvroConvert
    {
        public static byte[] Serialize(object obj)
        {

            Dupa dupa = new Dupa
            {
                cycek = new Cycki
                {
                    lewy = "spoko",
                    prawy = 2137
                },
                numebr = 111111
            };

            Dupa dupa2 = new Dupa
            {
                cycek = new Cycki
                {
                    lewy = "loko",
                    prawy = 2137
                },
                numebr = 2135
            };

            var xd = AvroSerializer.Create<Dupa>().WriterSchema.ToString();

            MemoryStream resultStream = new MemoryStream();
            var writer = Writer.OpenWriter(new GenericDatumWriter(Schema.Parse(xd)), resultStream);


            writer.Append(dupa);
            writer.Append(dupa2);


            var writer2 = Encoder.Writer.OpenWriter(new Encoder.GenericDatumWriter(Schema.Parse(xd)), "result.avro");
            writer2.Append(dupa);
            writer2.Append(dupa2);
            writer2.Close();

            //            using (MemoryStream ms = new MemoryStream())
            //            {
            //                resultStream.CopyTo(ms);
            //
            //                var xd3 = resultStream;
            //
            //                return ms.ToArray();
            //            }

            var result = resultStream.ToArray();

            writer.Close();

            return result;
        }
    }

    [DataContract(Name = "Dupa", Namespace = "test.demo")]
    public class Dupa
    {
        [DataMember(Name = "cycek")]
        public Cycki cycek { get; set; }

        [DataMember(Name = "numebr")]
        public int numebr { get; set; }
    }

    [DataContract(Name = "Cycek", Namespace = "pubsub.demo")]
    public class Cycki
    {
        [DataMember(Name = "lewy")]
        public string lewy { get; set; }
        [DataMember(Name = "prawy")]
        public long prawy { get; set; }
    }
}
