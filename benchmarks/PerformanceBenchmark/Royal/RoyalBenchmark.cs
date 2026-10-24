using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using GrandeBenchmark.BattleRoyale.Models;

namespace GrandeBenchmark.BattleRoyale;

//Reference: https://github.com/Im5tu/SerializationBenchmarks

[MemoryDiagnoser]
[MinColumn, MaxColumn]
[Config(typeof(Config))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public partial class RoyalBenchmark
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddColumn(new FileSizeColumn());
        }
    }

    public IEnumerable<DataSet> GenerateDataSets()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options.Converters.Add(new JsonStringEnumConverter());
        var sets = new[]
        {
            new { File = "Data_Small.json", Subset = "Small" },
            new { File = "Data_Medium.json", Subset = "Medium" },
            // new { File = "Data_Large.json", Subset = "Large" },
        };

        foreach (var set in sets)
        {
            var json = File.ReadAllText(set.File);
            var users = JsonSerializer.Deserialize<List<TestModel>>(json, options);

            var dataset = new DataSet
            {
                Name = set.Subset,
                Payload = users,
                SerializedData = new()
            };

            AvroConvert_Serialize(dataset);
            GroBuf_Serialize(dataset);
            yield return dataset;
        }
    }

    private void LogSize(string benchmarkName, int size)
    {
        var path = $"C:\\test\\disk-size.{benchmarkName.ToLower()}.txt";
        File.WriteAllText(path, $"{size / 1024} kB");
    }

    public class DataSet
    {
        public string Name { get; set; }
        public List<TestModel> Payload { get; set; }
        public SerializedData SerializedData { get; set; }
        public override string ToString() => Name;
    }

    public class SerializedData
    {
        public byte[] AvroConvert { get; set; }
        public byte[] GroBuf { get; set; }
    }
}