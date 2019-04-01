using System;

namespace AvroConvert
{
    using System.Runtime.Serialization;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");



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

            var serialized = AvroConvert.Serialize(dupa);

            var avroString = System.IO.File.ReadAllBytes("result.avro");


            var dx = AvroConvert.Deserialize(serialized);
        }
    }

    class User
    {
        public string name { get; set; }
        public int? favorite_number { get; set; }
        public string favorite_color { get; set; }
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
