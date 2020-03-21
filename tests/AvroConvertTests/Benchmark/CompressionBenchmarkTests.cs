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
        private readonly string rawDataset = File.ReadAllText("Benchmark/big_dataset.json");


        [Fact]
        public void Compression_CompareSizesAndTime_NoteResults()
        {
            //Arrange
            List<Dataset> datasets = JsonConvert.DeserializeObject<List<Dataset>>(rawDataset);

            //Act
            var json = new BenchmarkResult();
            json.Name = "Json";
            json.Size = 9945 * 1024;

            Stopwatch stopwatch = Stopwatch.StartNew();
            var data = JsonConvert.SerializeObject(datasets);
            json.SerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            JsonConvert.DeserializeObject<List<Dataset>>(data);
            json.DeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            File.WriteAllText("10mega.json", rawDataset);


            var avro = RunBenchmark(datasets, CodecType.Null);
            avro.Name = "Avro";
            var deflate = RunBenchmark(datasets, CodecType.Deflate);
            var snappy = RunBenchmark(datasets, CodecType.Snappy);
            var gzip = RunBenchmark(datasets, CodecType.GZip);


            File.WriteAllText("times.json",
                                ConstructLog(json) +
                                        ConstructLog(avro) +
                                        ConstructLog(deflate) +
                                        ConstructLog(snappy) +
                                        ConstructLog(gzip));
        }

        private BenchmarkResult RunBenchmark(List<Dataset> datasets, CodecType codec)
        {
            var result = new BenchmarkResult();
            result.Name = codec.ToString();

            Stopwatch stopwatch = Stopwatch.StartNew();

            //Serialize
            var avro = AvroConvert.Serialize(datasets, codec);
            result.SerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize
            AvroConvert.Deserialize1<List<Dataset>>(avro);
            result.DeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Serialize
            var avro2 = AvroConvert.Serialize(datasets, codec);
            result.SerializeTime2 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize
            AvroConvert.Deserialize<List<Dataset>>(avro2);
            result.DeserializeTime2 = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            //Size
            File.WriteAllBytes($"10mega.{result.Name}.avro", avro);
            result.Size = avro.Length;
            result.Size2 = avro2.Length;

            return result;
        }

        private string ConstructLog(BenchmarkResult result)
        {
            return $"{result.Name}: Serialize: {result.SerializeTime} ms {result.Size / 1024} kB; Deserialize: {result.DeserializeTime} ms Serialize2: {result.SerializeTime2} Size2: {result.Size2 / 1024} kB; ms Deserialize2: {result.DeserializeTime2} ms \n ";
        }
    }
}
