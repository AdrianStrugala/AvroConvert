using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using GrandeBenchmark;
using Newtonsoft.Json;
using SolTechnology.Avro;

namespace SolTechnology.PerformanceBenchmark
{
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class JustInTimeRunner
    {
        private class Config : ManualConfig
        {
            public Config() => AddColumn(new FileSizeColumn());
        }

        private const int N = 30;
        private readonly User[] data;

        public JustInTimeRunner()
        {
            Fixture fixture = new Fixture();
            data = fixture.Build<User>().With(u => u.Offerings, fixture.CreateMany<Offering>(N).ToList).CreateMany(N).ToArray();
        }

        [Benchmark]
        public void Avro_Default()
        {
            var serialized = AvroConvert.Serialize(data);
            AvroConvert.Deserialize<List<User>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Default).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }

        [Benchmark]
        public void Json_Default()
        {
            var serialized = JsonConvert.SerializeObject(data);
            JsonConvert.DeserializeObject<List<User>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Json_Default).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }

        private string ConstructSizeLog(int size)
        {
            return $"{size / 1024} kB";
        }
    }
}