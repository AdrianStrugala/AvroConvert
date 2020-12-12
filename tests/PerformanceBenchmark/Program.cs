using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AutoFixture;
using Avro;
using Avro.Generic;
using Avro.IO;
using SolTechnology.Avro;
using SolTechnology.Avro.FileHeader.Codec;

namespace SolTechnology.PerformanceBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {

            IntegrationTest.Invoke();

            var fixture = new Fixture();
            var data = fixture.CreateMany<Dataset>(30000).ToArray();


            Console.WriteLine("AvroConvert Benchmark - fire!");
            Console.WriteLine("");
            Console.WriteLine("The Benchmark compares Apache.Avro, AvroConvert (nuget version) and AvroConvert local version");

            int noOfRuns = 30;
            Console.WriteLine($"Number of runs: {noOfRuns}");

            string schema = AvroConvert.GenerateSchema(typeof(Dataset[]));


            //Act
            List<BenchmarkResult> benchmarkResults = new List<BenchmarkResult>();
            for (int i = 0; i < noOfRuns; i++)
            {
                benchmarkResults.Add(RunBenchmark(data, schema));
                if ((i + 1) % 10 == 0)
                {
                    Console.WriteLine($"Progress: {i + 1}/{noOfRuns}");
                }

            }


            BenchmarkResult mean = new BenchmarkResult();
            mean.AvroConvertVNextGzipSerializeTime = benchmarkResults.Sum(r => r.AvroConvertVNextGzipSerializeTime) / noOfRuns;
            mean.AvroConvertVNextGzipDeserializeTime = benchmarkResults.Sum(r => r.AvroConvertVNextGzipDeserializeTime) / noOfRuns;
            mean.AvroConvertVNextGzipSize = benchmarkResults.Sum(r => r.AvroConvertVNextGzipSize) / noOfRuns;

            mean.ApacheAvroSerializeTime = benchmarkResults.Sum(r => r.ApacheAvroSerializeTime) / noOfRuns;
            mean.ApacheAvroDeserializeTime = benchmarkResults.Sum(r => r.ApacheAvroDeserializeTime) / noOfRuns;
            mean.ApacheAvroSize = benchmarkResults.Sum(r => r.ApacheAvroSize) / noOfRuns;

            mean.AvroConvertHeadlessSerializeTime = benchmarkResults.Sum(r => r.AvroConvertHeadlessSerializeTime) / noOfRuns;
            mean.AvroConvertHeadlessDeserializeTime = benchmarkResults.Sum(r => r.AvroConvertHeadlessDeserializeTime) / noOfRuns;
            mean.AvroConvertHeadlessSize = benchmarkResults.Sum(r => r.AvroConvertHeadlessSize) / noOfRuns;

            mean.AvroConvertGzipSerializeTime = benchmarkResults.Sum(r => r.AvroConvertGzipSerializeTime) / noOfRuns;
            mean.AvroConvertGzipDeserializeTime = benchmarkResults.Sum(r => r.AvroConvertGzipDeserializeTime) / noOfRuns;
            mean.AvroConvertGzipSize = benchmarkResults.Sum(r => r.AvroConvertGzipSize) / noOfRuns;

            mean.AvroConvertVNextHeadlessSerializeTime = benchmarkResults.Sum(r => r.AvroConvertVNextHeadlessSerializeTime) / noOfRuns;
            mean.AvroConvertVNextHeadlessDeserializeTime = benchmarkResults.Sum(r => r.AvroConvertVNextHeadlessDeserializeTime) / noOfRuns;
            mean.AvroConvertVNextSize = benchmarkResults.Sum(r => r.AvroConvertVNextSize) / noOfRuns;

            ConstructLog(mean);

            Console.ReadLine();
        }

        private static BenchmarkResult RunBenchmark(Dataset[] datasets, string schema)
        {
            var result = new BenchmarkResult();

            Stopwatch stopwatch = Stopwatch.StartNew();


            //Serialize Apache.Avro
            MemoryStream apacheAvroSerializeStream = new MemoryStream();
            var encoder = new BinaryEncoder(apacheAvroSerializeStream);
            var apacheSchema = Schema.Parse(AvroConvert.GenerateSchema(typeof(Dataset)));

            var apacheWriter = new GenericDatumWriter<GenericRecord>(apacheSchema);

            foreach (var dataset in datasets)
            {
                apacheWriter.Write(ApacheAvroHelpers.Create(dataset, apacheSchema), encoder);
            }

            var apacheAvro = apacheAvroSerializeStream.ToArray();
            result.ApacheAvroSerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize Apache.Avro

            List<Dataset> apacheResult = new List<Dataset>();

            using (var ms = new MemoryStream(apacheAvro))
            {
                apacheSchema = Schema.Parse(AvroConvert.GenerateSchema(typeof(Dataset)));
                var apacheReader = new GenericDatumReader<GenericRecord>(apacheSchema, apacheSchema);
                var decoder = new BinaryDecoder(ms);
                foreach (var dataset in datasets)
                {
                    apacheResult.Add(ApacheAvroHelpers.Decreate<Dataset>(apacheReader.Read(null, decoder)));
                }
            }
            result.ApacheAvroDeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();


            //Serialize AvroConvert Headerless
            var avroHeadless = AvroConvert.SerializeHeadless(datasets, schema);
            result.AvroConvertHeadlessSerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize AvroConvert Headerless
            AvroConvert.DeserializeHeadless<List<Dataset>>(avroHeadless, schema);
            result.AvroConvertHeadlessDeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();


            //Serialize AvroConvert Gzip
            var avroGzip = AvroConvert.Serialize(datasets, CodecType.GZip);
            result.AvroConvertGzipSerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize AvroConvert Gzip
            AvroConvert.Deserialize<Dataset[]>(avroGzip);
            result.AvroConvertGzipDeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();


            //Serialize AvroConvert vNext
            var newAvro = AvroConvertToUpdate.AvroConvert.SerializeHeadless(datasets, schema);
            result.AvroConvertVNextHeadlessSerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize AvroConvert vNext
            AvroConvertToUpdate.AvroConvert.DeserializeHeadless<Dataset[]>(newAvro, schema);
            result.AvroConvertVNextHeadlessDeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            //Serialize AvroConvert vNext Gzip
            var newAvroGzip = AvroConvertToUpdate.AvroConvert.SerializeHeadless(datasets, schema, AvroConvertToUpdate.Codec.CodecType.GZip);
            result.AvroConvertVNextGzipSerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize AvroConvert vNext Gzip
            AvroConvertToUpdate.AvroConvert.DeserializeHeadless<Dataset[]>(newAvroGzip, schema);
            result.AvroConvertVNextGzipDeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();


            //Size
            result.ApacheAvroSize = apacheAvro.Length;
            result.AvroConvertHeadlessSize = avroHeadless.Length;
            result.AvroConvertGzipSize = avroGzip.Length;
            result.AvroConvertVNextSize = newAvro.Length;
            result.AvroConvertVNextGzipSize = newAvroGzip.Length;

            return result;
        }

        private static void ConstructLog(BenchmarkResult result)
        {
            Console.WriteLine("");
            Console.WriteLine($"Apache.Avro:     Serialize: {result.ApacheAvroSerializeTime} ms {result.ApacheAvroSize / 1024} kB; Deserialize: {result.ApacheAvroDeserializeTime} ms");
            Console.WriteLine($"Avro Headless:   Serialize: {result.AvroConvertHeadlessSerializeTime} ms {result.AvroConvertHeadlessSize / 1024} kB; Deserialize: {result.AvroConvertHeadlessDeserializeTime} ms");
            Console.WriteLine($"Avro Gzip:       Serialize: {result.AvroConvertGzipSerializeTime} ms {result.AvroConvertGzipSize / 1024} kB; Deserialize: {result.AvroConvertGzipDeserializeTime} ms");
            Console.WriteLine($"Avro vNext:      Serialize: {result.AvroConvertVNextHeadlessSerializeTime} ms {result.AvroConvertVNextSize / 1024} kB; Deserialize: {result.AvroConvertVNextHeadlessDeserializeTime} ms");
            Console.WriteLine($"Avro vNext Gzip: Serialize: {result.AvroConvertVNextGzipSerializeTime} ms {result.AvroConvertVNextGzipSize / 1024} kB; Deserialize: {result.AvroConvertVNextGzipDeserializeTime} ms");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Summarize:");
            Console.WriteLine("Let's produce one value for each of the records. Assume internet speed 100mb/s \nand calculate how fast the data is send between to microservices:");
            Console.WriteLine("");

            //How the time is calculated: size to megabits => divide by network speed => multiply x 1000 to have ms 
            double jsonTime = (double)result.AvroConvertVNextGzipSize * 8 * 1000 / (1024 * 1024 * 100); // ms

            Console.WriteLine($"Apache.Avro:      {result.ApacheAvroSerializeTime + ((double)result.ApacheAvroSize * 8 * 1000 / (1024 * 1024 * 100)) + result.ApacheAvroDeserializeTime} ms");
            Console.WriteLine($"Avro Headless:    {result.AvroConvertHeadlessSerializeTime + ((double)result.AvroConvertHeadlessSize * 8 * 1000 / (1024 * 1024 * 100)) + result.AvroConvertHeadlessDeserializeTime} ms");
            Console.WriteLine($"Avro Gzip:        {result.AvroConvertGzipSerializeTime + ((double)result.AvroConvertGzipSize * 8 * 1000 / (1024 * 1024 * 100)) + result.AvroConvertGzipDeserializeTime} ms");
            Console.WriteLine($"Avro vNext:       {result.AvroConvertVNextHeadlessSerializeTime + ((double)result.AvroConvertVNextSize * 8 * 1000 / (1024 * 1024 * 100)) + result.AvroConvertVNextHeadlessDeserializeTime} ms");
            Console.WriteLine($"Avro vNext Gzip:  {result.AvroConvertVNextGzipSerializeTime + jsonTime + result.AvroConvertVNextGzipDeserializeTime} ms");
        }
    }
}
