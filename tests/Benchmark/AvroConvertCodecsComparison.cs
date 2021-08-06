using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using SolTechnology.Avro;

namespace GrandeBenchmark
{
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class AvroConvertCodecsComparison
    {
        private class Config : ManualConfig
        {
            public Config() => AddColumn(new FileSizeColumn());
        }

        private const int N = 100;
        private readonly User[] data;

        public AvroConvertCodecsComparison()
        {
            Fixture fixture = new Fixture();
            data = fixture.CreateMany<User>(N).ToArray();
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
        public void Avro_Gzip()
        {
            var serialized = AvroConvert.Serialize(data, CodecType.GZip);
            AvroConvert.Deserialize<List<User>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Gzip).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }

        [Benchmark]
        public void Avro_Snappy()
        {
            var serialized = AvroConvert.Serialize(data, CodecType.Snappy);
            AvroConvert.Deserialize<List<User>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Snappy).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }

        [Benchmark]
        public void Avro_Deflate()
        {
            var serialized = AvroConvert.Serialize(data, CodecType.Deflate);
            AvroConvert.Deserialize<List<User>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Deflate).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }

        [Benchmark]
        public void Avro_Brotli()
        {
            var serialized = AvroConvert.Serialize(data, CodecType.Brotli);
            AvroConvert.Deserialize<List<User>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Brotli).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }


        private string ConstructSizeLog(int size)
        {
            return $"{size / 1024} kB";
        }
    }
}