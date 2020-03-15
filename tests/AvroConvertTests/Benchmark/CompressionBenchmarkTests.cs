using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using SolTechnology.Avro;
using SolTechnology.Avro.Codec;
using Xunit;

namespace AvroConvertTests.Benchmark
{
    public class CompressionBenchmarkTests
    {
        private readonly string rawDataset = System.IO.File.ReadAllText("Benchmark/big_dataset.json");


        [Fact]
        public void Compression_CompareSizesAndTime_VerifyDataQuality()
        {
            //Arrange
            List<Dataset> datasets = JsonConvert.DeserializeObject<List<Dataset>>(rawDataset);

            //Act
            Stopwatch stopwatch = Stopwatch.StartNew();
            var json = JsonConvert.SerializeObject(datasets);
            var timeJson = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            File.WriteAllText("10mega.json", rawDataset);


            var avro = RunBenchmark(datasets, CodecType.Null);
            var deflate = RunBenchmark(datasets, CodecType.Deflate);
            var snappy = RunBenchmark(datasets, CodecType.Snappy);
            var gzip = RunBenchmark(datasets, CodecType.GZip);


            File.WriteAllText("times.json", $"Json: {timeJson} ms 9945 kB \n" +
                                            $"Avro: {avro.Time} ms {avro.Size / 1024} kB \n" +
                                            $"{deflate.Name}: {deflate.Time} ms {deflate.Size / 1024} kB \n" +
                                            $"{snappy.Name}: {snappy.Time} ms {snappy.Size / 1024} kB \n" +
                                            $"{gzip.Name}: {gzip.Time} ms {gzip.Size / 1024} kB \n");

            //Assert
        }

        private BenchmarkResult RunBenchmark(List<Dataset> datasets, CodecType codec)
        {
            var result = new BenchmarkResult();
            result.Name = codec.ToString();

            Stopwatch stopwatch = Stopwatch.StartNew();
            var avro = AvroConvert.Serialize(datasets, codec);
            stopwatch.Stop();

            File.WriteAllBytes($"10mega.{result.Name}.avro", avro);

            result.Time = stopwatch.ElapsedMilliseconds;
            result.Size = avro.Length;

            return result;
        }
    }
}
