using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using BenchmarkDotNet.Attributes;
using GrandeBenchmark.BattleRoyale.Models;

namespace GrandeBenchmark.BattleRoyale;

public partial class RoyalBenchmark
{
    private readonly Newtonsoft.Json.JsonSerializerSettings _newtonsoftSettings 
        = new() { ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver { NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy() } };
    
    [Benchmark, BenchmarkCategory("Serialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public byte[] NewtonsoftJsonGzip_Serialize(DataSet data)
    {
        var result = GzipJson(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(data.Payload, _newtonsoftSettings)));
        data.SerializedData.NewtonsoftJsonGzip = result;

        LogSize(nameof(NewtonsoftJsonGzip_Serialize), result.Length);

        return result;
    }
    
    [Benchmark, BenchmarkCategory("Deserialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public List<User> NewtonsoftJsonGzip_Deserialize(DataSet data)
    {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<List<User>>(Encoding.UTF8.GetString(UnGzipJson(data.SerializedData.NewtonsoftJsonGzip)), _newtonsoftSettings);
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
}