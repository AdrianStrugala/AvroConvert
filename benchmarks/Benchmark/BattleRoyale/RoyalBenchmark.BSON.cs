using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;
using GrandeBenchmark.BattleRoyale.Models;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace GrandeBenchmark.BattleRoyale;

public partial class RoyalBenchmark
{
    [Benchmark, BenchmarkCategory("Serialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public byte[] BSON_Serialize(DataSet data)
    {
        var result = DataConvert_BSON(data.Payload);
        data.SerializedData.BSON = result;

        LogSize(nameof(BSON_Serialize), result.Length);

        return result;
    }

    [Benchmark, BenchmarkCategory("Deserialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public List<User> BSON_Deserialize(DataSet data)
    {
        return BsonSerializer.Deserialize<UserWrapper>(data.SerializedData.BSON).Users;
    }

    // We need this because we cannot write an array to the root of a bson document
    public record UserWrapper(List<User> Users);

    private byte[] DataConvert_BSON(List<User> users)
    {
        using var ms = new MemoryStream();
        var writer = new BsonBinaryWriter(ms);
        BsonSerializer.Serialize(writer, new UserWrapper(users));
        return ms.ToArray();
    }
}