using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using GrandeBenchmark.BattleRoyale.Models;
using GroBuf;
using GroBuf.DataMembersExtracters;
using Serializer = GroBuf.Serializer;

namespace GrandeBenchmark.BattleRoyale;

public partial class RoyalBenchmark
{
    private readonly Serializer _groSerializer = new Serializer(new PropertiesExtractor(), options : GroBufOptions.WriteEmptyObjects);

    
    [Benchmark, BenchmarkCategory("Serialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public byte[] GroBuf_Serialize(DataSet data)
    {
        var result = _groSerializer.Serialize(data.Payload);
        data.SerializedData.GroBuf = result;

        LogSize(nameof(GroBuf_Serialize), result.Length);

        return result;
    }
    
    [Benchmark, BenchmarkCategory("Deserialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public List<User> GroBuf_Deserialize(DataSet data)
    {
        return _groSerializer.Deserialize<List<User>>(data.SerializedData.GroBuf);
    }
}