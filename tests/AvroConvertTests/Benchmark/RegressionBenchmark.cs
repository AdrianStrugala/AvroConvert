using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AutoFixture;
using SolTechnology.Avro;
using SolTechnology.Avro.Codec;
using Xunit;

namespace AvroConvertTests.Benchmark
{
    public class RegressionBenchmark
    {
        public void Regression_CompareSizesAndTime_NoteResults()
        {
            //Arrange
            var fixture = new Fixture();
            List<Dataset> datasets = fixture.CreateMany<Dataset>(100000).ToList();

            int noOfRuns = 20;

            //Act
            List<BenchmarkResult> benchmarkResults = new List<BenchmarkResult>();
            for (int i = 0; i < noOfRuns; i++)
            {
                benchmarkResults.Add(RunBenchmark(datasets, CodecType.GZip));
            }

            BenchmarkResult mean = new BenchmarkResult();
            mean.DeserializeTime = benchmarkResults.Sum(r => r.DeserializeTime) / noOfRuns;
            mean.DeserializeTime2 = benchmarkResults.Sum(r => r.DeserializeTime2) / noOfRuns;
            mean.SerializeTime = benchmarkResults.Sum(r => r.SerializeTime) / noOfRuns;
            mean.SerializeTime2 = benchmarkResults.Sum(r => r.SerializeTime2) / noOfRuns;
            mean.Size = benchmarkResults.Sum(r => r.Size) / noOfRuns;
            mean.Size2 = benchmarkResults.Sum(r => r.Size2) / noOfRuns;

            mean.DeserializeTime3 = benchmarkResults.Sum(r => r.DeserializeTime3) / noOfRuns;
            mean.SerializeTime3 = benchmarkResults.Sum(r => r.SerializeTime3) / noOfRuns;


            File.WriteAllText("regression.json", ConstructLog(mean));
        }

        private BenchmarkResult RunBenchmark(List<Dataset> datasets, CodecType codec)
        {
            var result = new BenchmarkResult();
            result.Name = codec.ToString();

            Stopwatch stopwatch = Stopwatch.StartNew();

            //Serialize Apache.Avro
            var avro = AvroConvert.SerializeOrig(datasets, codec);
            result.SerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize Apache.Avro
            AvroConvert.DeserializeOrig<List<Dataset>>(avro);
            result.DeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Serialize AvroConvert 2.2.2
            var avro3 = AvroConvert.Serialize1(datasets, codec);
            result.SerializeTime3 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize AvroConvert 2.2.2
            AvroConvert.Deserialize1<List<Dataset>>(avro3);
            result.DeserializeTime3 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Serialize AvroConvert 2.4.0
            var avro2 = AvroConvert.Serialize(datasets, codec);
            result.SerializeTime2 = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize AvroConvert 2.4.0
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
            return $@"
Apache.Avro: Serialize: {result.SerializeTime} ms {result.Size / 1024} kB; Deserialize: {result.DeserializeTime} ms \n 
AvroConvert 2.2.2 Serialize: {result.SerializeTime3} ms Size: {result.Size2 / 1024} kB; ms Deserialize: {result.DeserializeTime3} ms \n 
AvroConvert 2.4.0 Serialize: {result.SerializeTime2} ms Size: {result.Size2 / 1024} kB; ms Deserialize: {result.DeserializeTime2} ms \n ";
        }
    }
}
