using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avro;
using Avro.Generic;
using Avro.IO;
using Avro.Specific;
using Newtonsoft.Json;
using SolTechnology.Avro;
using SolTechnology.Avro.Codec;
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Write.Resolvers;

namespace SolTechnology.PerformanceBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            //TODO: new input object (prob autofixture). Avro sucks

            string rawDataset = File.ReadAllText("10mb_dataset.json");
            var data = JsonConvert.DeserializeObject<Dataset[]>(rawDataset);


            Console.WriteLine("AvroConvert Benchmark - fire!");
            Console.Write("The Benchmark compares Newtonsoft.Json, Apache.Avro, AvroConvert (nuget version) and AvroConvert local version");

            int noOfRuns = 10;
            Console.WriteLine($"Number of runs: {noOfRuns}");

            string schema = AvroConvert.GenerateSchema(typeof(Dataset[]));


            //Act
            List<BenchmarkResult> benchmarkResults = new List<BenchmarkResult>();
            for (int i = 0; i < noOfRuns; i++)
            {
                benchmarkResults.Add(RunBenchmark(data, schema));
                Console.WriteLine($"Run: {i + 1}/{noOfRuns}");
            }


            BenchmarkResult mean = new BenchmarkResult();
            mean.JsonSerializeTime = benchmarkResults.Sum(r => r.JsonSerializeTime) / noOfRuns;
            mean.JsonDeserializeTime = benchmarkResults.Sum(r => r.JsonDeserializeTime) / noOfRuns;
            mean.JsonSize = benchmarkResults.Sum(r => r.JsonSize) / noOfRuns;

            mean.ApacheAvroSerializeTime = benchmarkResults.Sum(r => r.ApacheAvroSerializeTime) / noOfRuns;
            mean.ApacheAvroDeserializeTime = benchmarkResults.Sum(r => r.ApacheAvroDeserializeTime) / noOfRuns;
            mean.ApacheAvroSize = benchmarkResults.Sum(r => r.ApacheAvroSize) / noOfRuns;

            mean.AvroConvertHeadlessSerializeTime = benchmarkResults.Sum(r => r.AvroConvertHeadlessSerializeTime) / noOfRuns;
            mean.AvroConvertHeadlessDeserializeTime = benchmarkResults.Sum(r => r.AvroConvertHeadlessDeserializeTime) / noOfRuns;
            mean.AvroConvertHeadlessSize = benchmarkResults.Sum(r => r.AvroConvertHeadlessSize) / noOfRuns;

            mean.AvroConvertDeflateSerializeTime = benchmarkResults.Sum(r => r.AvroConvertDeflateSerializeTime) / noOfRuns;
            mean.AvroConvertDeflateDeserializeTime = benchmarkResults.Sum(r => r.AvroConvertDeflateDeserializeTime) / noOfRuns;
            mean.AvroConvertDeflateSize = benchmarkResults.Sum(r => r.AvroConvertDeflateSize) / noOfRuns;

            mean.AvroConvertVNextSerializeTime = benchmarkResults.Sum(r => r.AvroConvertVNextSerializeTime) / noOfRuns;
            mean.AvroConvertVNextDeserializeTime = benchmarkResults.Sum(r => r.AvroConvertVNextDeserializeTime) / noOfRuns;
            mean.AvroConvertVNextSize = benchmarkResults.Sum(r => r.AvroConvertVNextSize) / noOfRuns;

            ConstructLog(mean);

            Console.ReadLine();
        }

        private static BenchmarkResult RunBenchmark(Dataset[] datasets, string schema)
        {
            var result = new BenchmarkResult();

            Stopwatch stopwatch = Stopwatch.StartNew();

            //Serialize Json
            var data = JsonConvert.SerializeObject(datasets);
            result.JsonSerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize Json
            JsonConvert.DeserializeObject<Dataset[]>(data);
            result.JsonDeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();


            //Serialize Apache.Avro
            MemoryStream apacheAvroSerializeStream = new MemoryStream();
            var encoder = new BinaryEncoder(apacheAvroSerializeStream);
            var apacheSchema = Schema.Parse(AvroConvert.GenerateSchema(typeof(Dataset)));

            var apacheWriter = new GenericDatumWriter<GenericRecord>(apacheSchema);

            foreach (var dataset in datasets)
            {
                apacheWriter.Write(Create(dataset, apacheSchema), encoder);
            }

            var apacheAvro = apacheAvroSerializeStream.ToArray();
            result.ApacheAvroSerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize Apache.Avro
            using (var ms = new MemoryStream(apacheAvro))
            {
                apacheSchema = Schema.Parse(AvroConvert.GenerateSchema(typeof(Dataset)));
                var apacheReader = new GenericDatumReader<GenericRecord>(apacheSchema, apacheSchema);
                var decoder = new BinaryDecoder(ms);
                apacheReader.Read(null, decoder);
                result.ApacheAvroDeserializeTime = stopwatch.ElapsedMilliseconds;
                stopwatch.Restart();
            }


            //Serialize AvroConvert Headerless
            //            var avroHeadless = AvroConvert.SerializeHeadless(datasets, schema, codec);
            result.AvroConvertHeadlessSerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize AvroConvert Headerless
            //            AvroConvert.DeserializeHeadless<List<Dataset>>(avroHeadless, schema);
            result.AvroConvertHeadlessDeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();


            //Serialize AvroConvert Deflate
            var avroDeflate = AvroConvert.Serialize(datasets, CodecType.Deflate);
            result.AvroConvertDeflateSerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize AvroConvert Deflate
            AvroConvert.Deserialize<Dataset[]>(avroDeflate);
            result.AvroConvertDeflateDeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();


            //Serialize AvroConvert vNext
            var newAvro = AvroConvertToUpdate.AvroConvert.SerializeHeadless(datasets, schema);
            result.AvroConvertVNextSerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Deserialize AvroConvert vNext
            AvroConvertToUpdate.AvroConvert.DeserializeHeadless<Dataset[]>(newAvro, schema);
            result.AvroConvertVNextDeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();


            //Size
            result.JsonSize = 9945 * 1024;
            result.ApacheAvroSize = apacheAvro.Length;
            result.AvroConvertDeflateSize = avroDeflate.Length;
            result.AvroConvertVNextSize = newAvro.Length;

            return result;
        }

        private static void ConstructLog(BenchmarkResult result)
        {
            Console.WriteLine($"Json:          Serialize: {result.JsonSerializeTime} ms {result.JsonSize / 1024} kB; Deserialize: {result.JsonDeserializeTime} ms");
            Console.WriteLine($"Apache.Avro:   Serialize: {result.ApacheAvroSerializeTime} ms {result.ApacheAvroSize / 1024} kB; Deserialize: {result.ApacheAvroDeserializeTime} ms");
            Console.WriteLine($"Avro Headless: Serialize: {result.AvroConvertHeadlessSerializeTime} ms {result.AvroConvertHeadlessSize / 1024} kB; Deserialize: {result.AvroConvertHeadlessDeserializeTime} ms");
            Console.WriteLine($"Avro Deflate:  Serialize: {result.AvroConvertDeflateSerializeTime} ms {result.AvroConvertDeflateSize / 1024} kB; Deserialize: {result.AvroConvertDeflateDeserializeTime} ms");
            Console.WriteLine($"Avro vNext:    Serialize: {result.AvroConvertVNextSerializeTime} ms {result.AvroConvertVNextSize / 1024} kB; Deserialize: {result.AvroConvertVNextDeserializeTime} ms");
        }

        private static GenericRecord Create(object item, Schema schema)
        {
            var genericRecord = new GenericRecord((RecordSchema)schema);

            var props = item.GetType().GetProperties();

            foreach (var propertyInfo in props)
            {
                genericRecord.Add(propertyInfo.Name, propertyInfo.GetValue(item));
            }

            return genericRecord;
        }
    }
}
