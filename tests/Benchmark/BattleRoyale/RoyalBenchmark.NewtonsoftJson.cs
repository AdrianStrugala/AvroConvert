using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using GrandeBenchmark.BattleRoyale.Models;

namespace GrandeBenchmark.BattleRoyale;

public partial class RoyalBenchmark
{
    [Benchmark, BenchmarkCategory("Serialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public string NewtonsoftJson_Serialize(DataSet data)
    {
        var result = Newtonsoft.Json.JsonConvert.SerializeObject(data.Payload, _newtonsoftSettings);
        data.SerializedData.NewtonsoftJson = result;

        LogSize(nameof(NewtonsoftJson_Serialize), Encoding.UTF8.GetBytes(result).Length);

        return result;
    }
    
    [Benchmark, BenchmarkCategory("Deserialization", "Binary"), ArgumentsSource(nameof(GenerateDataSets))]
    public List<User> NewtonsoftJson_Deserialize(DataSet data)
    {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<List<User>>(data.SerializedData.NewtonsoftJson, _newtonsoftSettings);
    }
}