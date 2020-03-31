using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
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

            Stopwatch stopwatch = Stopwatch.StartNew();
            var data = JsonConvert.SerializeObject(datasets);
            json.SerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            JsonConvert.DeserializeObject<List<Dataset>>(data);
            json.DeserializeTime = stopwatch.ElapsedMilliseconds;

            json.Size = Encoding.UTF8.GetBytes(data).Length;
            File.WriteAllText("10mega.json", rawDataset);

            stopwatch.Restart();

            var jsonGzip = new BenchmarkResult();
            jsonGzip.Name = "JsonGzip";

            var jsonGzipData = JsonConvert.SerializeObject(datasets);
            var compressedJson = GzipJson(jsonGzipData);
            jsonGzip.SerializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();
            var uncompressedJson = UnGzipJson(compressedJson);
            JsonConvert.DeserializeObject<List<Dataset>>(uncompressedJson);
            jsonGzip.DeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            jsonGzip.Size = compressedJson.Length;
            File.WriteAllBytes("10mega.json.gz", compressedJson);



            var avro = RunBenchmark(datasets, CodecType.Null);
            avro.Name = "Avro";
            var deflate = RunBenchmark(datasets, CodecType.Deflate);
            var snappy = RunBenchmark(datasets, CodecType.Snappy);
            var gzip = RunBenchmark(datasets, CodecType.GZip);


            File.WriteAllText("times.json", ConstructLog(json) + ConstructLog(avro) + ConstructLog(deflate) + ConstructLog(snappy) + ConstructLog(gzip));
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
            AvroConvert.Deserialize<List<Dataset>>(avro);
            result.DeserializeTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            //Size
            File.WriteAllBytes($"10mega.{result.Name}.avro", avro);
            result.Size = avro.Length;

            return result;
        }

        private string ConstructLog(BenchmarkResult result)
        {
            return $"{result.Name}: Serialize: {result.SerializeTime} ms {result.Size / 1024} kB; Deserialize: {result.DeserializeTime} ms \n ";
        }

        static byte[] GzipJson(string json)
        {
            var data = Encoding.UTF8.GetBytes(json);
            using var compressedStream = new MemoryStream();
            using var zipStream = new GZipStream(compressedStream, CompressionMode.Compress);
            zipStream.Write(data, 0, data.Length);
            zipStream.Close();
            return compressedStream.ToArray();
        }

        static string UnGzipJson(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return Encoding.UTF8.GetString(resultStream.ToArray());
            }
        }
    }
}
