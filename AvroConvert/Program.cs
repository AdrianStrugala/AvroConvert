using System;

namespace AvroConvert
{
    using System.Runtime.Serialization;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");


            var serialized = AvroConvert.Serialize(1);

            var avroString = System.IO.File.ReadAllBytes("result.avro");


            var dx = AvroConvert.Deserialize(serialized);
        }
    }

}
