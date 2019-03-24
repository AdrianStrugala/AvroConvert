using System;

namespace AvroConvert
{
    using System.Text;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var avroString = System.IO.File.ReadAllBytes("example.avro");

            AvroConvert.Deserialize(avroString);
        }
    }
}
