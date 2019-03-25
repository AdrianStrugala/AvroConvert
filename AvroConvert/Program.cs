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


            var avroString = System.IO.File.ReadAllBytes("example2.avro");


            var reader = DataFileReader<User>.OpenReader(new MemoryStream(avroString), null);
           var header =  reader.GetHeader();

           var lol = reader.Next();

           var xd = reader.NextEntries;


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
