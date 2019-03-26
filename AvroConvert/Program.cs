using System;

namespace AvroConvert
{
    using System.IO;
    using System.Text;
    using Avro;
    using Avro.File;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            AvroConvert.Serialize(1);

            var avroString = System.IO.File.ReadAllBytes("example2.avro");


            AvroConvert.Deserialize(avroString);
        }
    }

    class User
    {
        public string name { get; set; }
        public int? favorite_number { get; set; }
        public string favorite_color { get; set; }
    }
}
