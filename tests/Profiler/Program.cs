using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using GrandeBenchmark;
using SolTechnology.Avro;

namespace Profiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Fixture fixture = new Fixture();
            var data = fixture.Build<User>().With(u => u.Offerings, fixture.CreateMany<Offering>(21).ToList).CreateMany(37).ToArray();

            var serialized = AvroConvert.Serialize(data);
            var deserialized = AvroConvert.Deserialize<List<User>>(serialized);

            Console.WriteLine("end World!");
            Console.ReadLine();
        }
    }
}
