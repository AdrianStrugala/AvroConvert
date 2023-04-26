using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolTechnology.Avro;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var xd = AvroConvert.GenerateSchema(typeof(string));

            var x = xd;
        }
    }
}
