using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AutoFixture;
using Newtonsoft.Json;
using SolTechnology.Avro;
using SolTechnology.Avro.Codec;
using Xunit;

namespace AvroConvertTests.Benchmark
{
    public class RegressionBenchmark
    {
        [Fact]
        public void Compression_CompareSizesAndTime_NoteResults()
        {
            //Arrange
            var fixture = new Fixture();
            List<Dataset> datasets = fixture.CreateMany<Dataset>(100000).ToList();

            int noOfRuns = 20;

            //Act
            List<BenchmarkResult> benchmarkResults = new List<BenchmarkResult>();
            for (int i = 0; i < noOfRuns; i++)
            {
                benchmarkResults.Add(RunBenchmark(datasets, CodecType.Null));
            }

            BenchmarkResult mean = new BenchmarkResult();
            mean.DeserializeTime = benchmarkResults.Sum(r => r.DeserializeTime) / noOfRuns;
            mean.DeserializeTime2 = benchmarkResults.Sum(r => r.DeserializeTime2) / noOfRuns;
            mean.SerializeTime = benchmarkResults.Sum(r => r.SerializeTime) / noOfRuns;
            mean.SerializeTime2 = benchmarkResults.Sum(r => r.SerializeTime2) / noOfRuns;
            mean.Size = benchmarkResults.Sum(r => r.Size) / noOfRuns;
            mean.Size2 = benchmarkResults.Sum(r => r.Size2) / noOfRuns;


            File.WriteAllText("regression.json", ConstructLog(mean));
        }

        private BenchmarkResult RunBenchmark(List<Dataset> datasets, CodecType codec)
        {
            var result = new BenchmarkResult();
            result.Name = codec.ToString();

            Stopwatch stopwatch = Stopwatch.StartNew();

            //Serialize
            var avro = AvroConvert.SerializeOrig(datasets, codec);
            result.SerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize
            AvroConvert.DeserializeOrig<List<Dataset>>(avro);
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
            result.Size = avro.Length;
            result.Size2 = avro2.Length;

            return result;
        }

        private string ConstructLog(BenchmarkResult result)
        {
            return $"ORIG: Serialize: {result.SerializeTime} ms {result.Size / 1024} kB; Deserialize: {result.DeserializeTime} ms  NEW Serialize: {result.SerializeTime2} ms Size: {result.Size2 / 1024} kB; ms Deserialize: {result.DeserializeTime2} ms \n ";
        }
    }
}
