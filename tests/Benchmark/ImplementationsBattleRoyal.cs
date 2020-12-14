using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using AutoFixture;
using Benchmark;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Newtonsoft.Json;
using AvroConvert = SolTechnology.Avro.AvroConvert;
using AvroConvertVNext = SolTechnology.PerformanceBenchmark.AvroConvertToUpdate;

namespace GrandeBenchmark
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

        private const int N = 200;
        private readonly User[] data;

        public ImplementationsBattleRoyal()
        {
            Fixture fixture = new Fixture();
            data = fixture
                .Build<User>()
                .With(c => c.Offerings, fixture.CreateMany<Offering>(N).ToList())
                .CreateMany(N)
                .ToArray();
        }



        [Benchmark]
        public void Avro_Nuget_Default()
        {
            var serialized = AvroConvert.Serialize(data);
            AvroConvert.Deserialize<List<User>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Nuget_Default).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }

        [Benchmark]
        public void Json_Default()
        {
            var serialized = JsonConvert.SerializeObject(data);
            JsonConvert.DeserializeObject<List<User>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Json_Default).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(Encoding.UTF8.GetBytes(serialized).Length));
        }

        [Benchmark]
        public void Avro_Local_Default()
        {
            var serialized = AvroConvertVNext.AvroConvert.Serialize(data);
            AvroConvertVNext.AvroConvert.Deserialize<List<User>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Local_Default).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }
        //
        // [Benchmark]
        // public void Avro_Nuget_Gzip()
        // {
        //     var serialized = AvroConvert.Serialize(data, CodecType.GZip);
        //     AvroConvert.Deserialize<List<User>>(serialized);
        //
        //     var path = $"C:\\test\\disk-size.{nameof(Avro_Nuget_Gzip).ToLower()}.txt";
        //     File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        // }
        //
        [Benchmark]
        public void Json_Gzip()
        {
            var serialized = JsonConvert.SerializeObject(data);
            var serializedBytes = Encoding.UTF8.GetBytes(serialized);
            var serializedGzip = GzipJson(serializedBytes);

            var deserializedBytes = UnGzipJson(serializedGzip);
            JsonConvert.DeserializeObject<List<User>>(Encoding.UTF8.GetString(deserializedBytes));


            var path = $"C:\\test\\disk-size.{nameof(Json_Gzip).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serializedGzip.Length));
        }


        [Benchmark]
        public void Avro_Local_Gzip()
        {
            var serialized = AvroConvertVNext.AvroConvert.Serialize(data, AvroConvertVNext.Codec.CodecType.GZip);
            AvroConvertVNext.AvroConvert.Deserialize<List<User>>(serialized);

            var path = $"C:\\test\\disk-size.{nameof(Avro_Local_Gzip).ToLower()}.txt";
            File.WriteAllText(path, ConstructSizeLog(serialized.Length));
        }
        //
        //
        // [Benchmark]
        // public void Json_Brotli()
        // {
        //     var serialized = JsonConvert.SerializeObject(data);
        //     var serializedBytes = Encoding.UTF8.GetBytes(serialized);
        //     var serializedGzip = Compress(serializedBytes);
        //
        //     var deserializedBytes = Decompress(serializedGzip);
        //     JsonConvert.DeserializeObject<List<User>>(Encoding.UTF8.GetString(deserializedBytes));
        //
        //
        //     var path = $"C:\\test\\disk-size.{nameof(Json_Brotli).ToLower()}.txt";
        //     File.WriteAllText(path, ConstructSizeLog(serializedGzip.Length));
        // }




        private string ConstructSizeLog(int size)
        {
            return $"{size / 1024} kB";
        }

        internal byte[] UnGzipJson(byte[] compressedData)
        {
            using (var compressedStream = new MemoryStream(compressedData))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        internal byte[] GzipJson(byte[] uncompressedData)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(uncompressedData, 0, uncompressedData.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        private byte[] Compress(byte[] uncompressedData)
        {
            using (var stream = new MemoryStream(uncompressedData))
            using (var mso = new MemoryStream())
            {
                using (var gs = new BrotliStream(mso, CompressionMode.Compress))
                {
                    stream.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }

        private byte[] Decompress(byte[] compressedData)
        {
            var mso = new MemoryStream();

            using (var gs = new GZipStream(new MemoryStream(compressedData), CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }

            mso.Position = 0;

            return mso.ToArray();
        }


        internal byte[] UnBrotliJson(byte[] compressedData)
        {
            using (var compressedStream = new MemoryStream(compressedData))
            using (var zipStream = new BrotliStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        internal byte[] BrotliJson(byte[] uncompressedData)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new BrotliStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(uncompressedData, 0, uncompressedData.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }
    }
}