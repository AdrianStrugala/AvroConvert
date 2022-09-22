using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using GrandeBenchmark.BattleRoyale.Models;
using SolTechnology.Avro;

namespace GrandeBenchmark.BattleRoyale;

public partial class RoyalBenchmark
{
    [Benchmark, BenchmarkCategory("Serialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public byte[] AvroConvert_Serialize(DataSet data)
    {
        var result = AvroConvert.Serialize(data.Payload);
        data.SerializedData.AvroConvert = result;

        LogSize(nameof(AvroConvert_Serialize), result.Length);

        return result;
    }

    [Benchmark, BenchmarkCategory("Deserialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public List<TestModel> AvroConvert_Deserialize(DataSet data)
    {
        return AvroConvert.Deserialize<List<TestModel>>(data.SerializedData.AvroConvert);
    }
}