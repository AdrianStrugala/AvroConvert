using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using GrandeBenchmark.BattleRoyale.Models;
using SolTechnology.Avro;

namespace GrandeBenchmark.BattleRoyale;

public partial class RoyalBenchmark
{
    [Benchmark, BenchmarkCategory("Serialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public byte[] AvroConvertBrotli_Serialize(DataSet data)
    {
        var result = AvroConvert.Serialize(data.Payload, CodecType.Brotli);
        data.SerializedData.AvroConvertBrotli = result;

        LogSize(nameof(AvroConvertBrotli_Serialize), result.Length);

        return result;
    }

    [Benchmark, BenchmarkCategory("Deserialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public List<User> AvroConvertBrotli_Deserialize(DataSet data)
    {
        return AvroConvert.Deserialize<List<User>>(data.SerializedData.AvroConvertBrotli);
    }
}