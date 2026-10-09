using System.Globalization;
using System.Text;
using Avro;
using Avro.File;
using Avro.Reflect;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Newtonsoft.Json;
using SolTechnology.Avro;

// AvroConvert vs Apache.Avro (Avro.Reflect, container file) vs Newtonsoft.Json (UTF-8 bytes) on the same POCOs.
// Run:  dotnet run -c Release -- --filter '*Libraries*' --artifacts ./artifacts
// Plot: python3 scripts/plot-libraries.py tests/LibrariesBenchmark/artifacts/results/LibrariesBenchmark.Libraries-report.csv

namespace LibrariesBenchmark;

public class Contact
{
    public int Id { get; set; }
    public string Address { get; set; }
    public long HouseNumber { get; set; }
    public string City { get; set; }
    public string PostCode { get; set; }
}

public class Offering
{
    public int Id { get; set; }
    public string ProductNumber { get; set; }
    public int Price { get; set; }
    public string Currency { get; set; }
    public bool Discount { get; set; }
}

public class User
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public Contact Contact { get; set; }
    public List<Offering> Offerings { get; set; }
}

public static class Dataset
{
    public const string ApacheSchema = """
        {"type":"record","name":"User","namespace":"LibrariesBenchmark","fields":[
          {"name":"Id","type":"int"},{"name":"IsActive","type":"boolean"},{"name":"Name","type":"string"},{"name":"Age","type":"int"},
          {"name":"Contact","type":{"type":"record","name":"Contact","fields":[
            {"name":"Id","type":"int"},{"name":"Address","type":"string"},{"name":"HouseNumber","type":"long"},{"name":"City","type":"string"},{"name":"PostCode","type":"string"}]}},
          {"name":"Offerings","type":{"type":"array","items":{"type":"record","name":"Offering","fields":[
            {"name":"Id","type":"int"},{"name":"ProductNumber","type":"string"},{"name":"Price","type":"int"},{"name":"Currency","type":"string"},{"name":"Discount","type":"boolean"}]}}}
        ]}
        """;

    public static List<User> Users(int count) => Enumerable.Range(0, count).Select(MakeUser).ToList();

    private static User MakeUser(int i) => new()
    {
        Id = i,
        IsActive = i % 2 == 0,
        Name = "user-" + i,
        Age = 20 + i % 50,
        Contact = new Contact { Id = i, Address = "Street " + i, HouseNumber = i, City = "City", PostCode = "00-000" },
        Offerings = new List<Offering>
        {
            new() { Id = i, ProductNumber = new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString(), Price = i, Currency = "PLN", Discount = i % 5 == 0 },
            new() { Id = i + 1, ProductNumber = new Guid(i + 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString(), Price = i * 2, Currency = "EUR", Discount = false }
        }
    };
}

public sealed class ApacheAvro
{
    private readonly Schema _schema = Schema.Parse(Dataset.ApacheSchema);
    private readonly ClassCache _cache = new();
    private readonly ReflectWriter<User> _writer;

    public ApacheAvro()
    {
        _cache.LoadClassCache(typeof(User), _schema);
        _writer = new ReflectWriter<User>(_schema, _cache);
    }

    public byte[] Serialize(List<User> users)
    {
        var stream = new MemoryStream();
        using (var file = DataFileWriter<User>.OpenWriter(_writer, stream, Codec.CreateCodec(Codec.Type.Null), leaveOpen: true))
        {
            foreach (var user in users)
            {
                file.Append(user);
            }
        }
        return stream.ToArray();
    }

    public List<User> Deserialize(byte[] avro)
    {
        using var file = DataFileReader<User>.OpenReader(new MemoryStream(avro), _schema, (ws, rs) => new ReflectReader<User>(ws, rs, _cache));
        var result = new List<User>();
        foreach (var user in file.NextEntries)
        {
            result.Add(user);
        }
        return result;
    }
}

public static class NewtonsoftJson
{
    private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault();

    public static byte[] Serialize(List<User> users)
    {
        var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true))
        using (var json = new JsonTextWriter(writer))
        {
            Serializer.Serialize(json, users);
        }
        return stream.ToArray();
    }

    public static List<User> Deserialize(byte[] json)
    {
        using var reader = new JsonTextReader(new StreamReader(new MemoryStream(json), Encoding.UTF8));
        return Serializer.Deserialize<List<User>>(reader);
    }
}

[MemoryDiagnoser]
[Config(typeof(Config))]
public class Libraries
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0).WithWarmupCount(3).WithIterationCount(10));
            AddColumn(new PayloadSizeColumn());
            SummaryStyle = SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend);
        }
    }

    [Params(1, 1_000, 20_000)]
    public int Records;

    private List<User> _users;
    private byte[] _avroConvert;
    private byte[] _apache;
    private byte[] _json;
    private ApacheAvro _apacheAvro;

    [GlobalSetup]
    public void Setup()
    {
        _users = Dataset.Users(Records);
        _apacheAvro = new ApacheAvro();
        _avroConvert = AvroConvert.Serialize(_users);
        _apache = _apacheAvro.Serialize(_users);
        _json = NewtonsoftJson.Serialize(_users);
    }

    [Benchmark] public byte[] AvroConvert_Serialize() => AvroConvert.Serialize(_users);
    [Benchmark] public List<User> AvroConvert_Deserialize() => AvroConvert.Deserialize<List<User>>(_avroConvert);

    [Benchmark] public byte[] ApacheAvro_Serialize() => _apacheAvro.Serialize(_users);
    [Benchmark] public List<User> ApacheAvro_Deserialize() => _apacheAvro.Deserialize(_apache);

    [Benchmark] public byte[] NewtonsoftJson_Serialize() => NewtonsoftJson.Serialize(_users);
    [Benchmark] public List<User> NewtonsoftJson_Deserialize() => NewtonsoftJson.Deserialize(_json);
}

/// <summary>Serialized payload size for the library/record count of each row, computed in the host process.</summary>
public sealed class PayloadSizeColumn : IColumn
{
    private static readonly Dictionary<(string, int), int> Cache = new();

    public string Id => nameof(PayloadSizeColumn);
    public string ColumnName => "Payload";
    public string Legend => "Size of the serialized data in bytes";
    public UnitType UnitType => UnitType.Size;
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Metric;
    public int PriorityInCategory => 0;
    public bool IsNumeric => true;
    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var library = benchmarkCase.Descriptor.WorkloadMethod.Name.Split('_')[0];
        var records = (int)benchmarkCase.Parameters["Records"];
        if (!Cache.TryGetValue((library, records), out var size))
        {
            var users = Dataset.Users(records);
            size = library switch
            {
                "AvroConvert" => AvroConvert.Serialize(users).Length,
                "ApacheAvro" => new ApacheAvro().Serialize(users).Length,
                "NewtonsoftJson" => NewtonsoftJson.Serialize(users).Length,
                _ => -1
            };
            Cache[(library, records)] = size;
        }
        return size < 0 ? "?" : size.ToString("N0", CultureInfo.InvariantCulture) + " B";
    }

    public override string ToString() => ColumnName;
}
