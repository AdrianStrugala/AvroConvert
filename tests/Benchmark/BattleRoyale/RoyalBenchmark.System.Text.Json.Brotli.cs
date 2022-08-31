using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using BenchmarkDotNet.Attributes;
using BrotliSharpLib;
using GrandeBenchmark.BattleRoyale.Models;

namespace GrandeBenchmark.BattleRoyale;

public partial class RoyalBenchmark
{
    [Benchmark(Baseline = true), BenchmarkCategory("Serialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public byte[] SystemTextJsonBrotli_Serialize(DataSet data)
    {
        var result = BrotliJson(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(data.Payload)));
        data.SerializedData.SystemTextJsonBrotli = result;

        LogSize(nameof(SystemTextJsonBrotli_Serialize), result.Length);

        return result;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Deserialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public List<User> SystemTextJsonBrotli_Deserialize(DataSet data)
    {
        return System.Text.Json.JsonSerializer.Deserialize<List<User>>(Encoding.UTF8.GetString(UnBrotliJson(data.SerializedData.SystemTextJsonBrotli)));
    }

    internal byte[] BrotliJson(byte[] uncompressedData)
    {
        return Brotli.CompressBuffer(uncompressedData, 0, uncompressedData.Length, 4);
    }

    internal byte[] UnBrotliJson(byte[] compressedData)
    {
        return Brotli.DecompressBuffer(compressedData, 0, compressedData.Length /**, customDictionary **/);
    }
}