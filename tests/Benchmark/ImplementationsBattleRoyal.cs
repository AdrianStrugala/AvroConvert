using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using AutoFixture;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Newtonsoft.Json;
using SolTechnology.Avro;
using SolTechnology.Avro.FileHeader.Codec;

using AvroConvertVNext = SolTechnology.PerformanceBenchmark.AvroConvertToUpdate;

namespace Benchmark
{
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class ImplementationsBattleRoyal
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddColumn(new FileSizeColumn());
            }
        }

        private const int N = 15000;
        private readonly Dataset[] data;

        public ImplementationsBattleRoyal()
        {
            Fixture fixture = new Fixture();
            data = fixture.CreateMany<Dataset>(N).ToArray();
        }



        [Benchmark]
        public void Avro_Nuget_Default()
        {
            var serialized = AvroConvert.Serialize(data);
            AvroConvert.Deserialize<List<Dataset>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Nuget_Default).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }

        [Benchmark]
        public void Json_Default()
        {
            var serialized = JsonConvert.SerializeObject(data);
            JsonConvert.DeserializeObject<List<Dataset>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Json_Default).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(Encoding.UTF8.GetBytes(serialized).Length));
        }

        [Benchmark]
        public void Avro_Local_Default()
        {
            var serialized = AvroConvertVNext.AvroConvert.Serialize(data);
            AvroConvertVNext.AvroConvert.Deserialize<List<Dataset>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Local_Default).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }

        [Benchmark]
        public void Avro_Nuget_Gzip()
        {
            var serialized = AvroConvert.Serialize(data, CodecType.GZip);
            AvroConvert.Deserialize<List<Dataset>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Nuget_Gzip).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }

        [Benchmark]
        public void Json_Gzip()
        {
            var serialized = JsonConvert.SerializeObject(data);
            var serializedBytes = Encoding.UTF8.GetBytes(serialized);
            var serializedGzip = Compress(serializedBytes);

            var deserializedBytes = Decompress(serializedGzip);
            JsonConvert.DeserializeObject<List<Dataset>>(Encoding.UTF8.GetString(deserializedBytes));


            var path = $"C:\\test\\disk-size.{nameof(Json_Gzip).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serializedGzip.Length));
        }


        [Benchmark]
        public void Avro_Local_Gzip()
        {
            var serialized = AvroConvertVNext.AvroConvert.Serialize(data, AvroConvertVNext.Codec.CodecType.GZip);
            AvroConvertVNext.AvroConvert.Deserialize<List<Dataset>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Local_Gzip).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }

        //todo add brotli




        private string ConstructSizeLog(int size)
        {
            return $"{size / 1024} kB";
        }

        internal byte[] Decompress(byte[] compressedData)
        {
            using (var compressedStream = new MemoryStream(compressedData))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        internal byte[] Compress(byte[] uncompressedData)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(uncompressedData, 0, uncompressedData.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }
    }
}