using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using SolTechnology.Avro;

// Compares released AvroConvert versions. 4.0 is resolved from ./local-packages (gitignored) – regenerate it with:
//   SKIP_TESTS=1 OUTPUT=tests/VersionsBenchmark/local-packages ./scripts/pack.sh
// Run:  dotnet run -c Release -- --filter '*AvroConvertVersions*' --artifacts ./artifacts
// Plot: python3 scripts/plot-versions.py tests/VersionsBenchmark/artifacts/results/VersionsBenchmark.AvroConvertVersions-report.csv

namespace VersionsBenchmark;

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
    public Guid ProductNumber { get; set; }
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

[MemoryDiagnoser]
[Config(typeof(Config))]
public class AvroConvertVersions
{
    private class Config : ManualConfig
    {
        public Config()
        {
            // Versions ordered by nuget.org downloads / milestones; 4.0 from the local package source.
            foreach (var version in new[] { "2.7.1", "3.2.9", "3.3.0", "3.4.8", "3.4.17", "4.0.0-preview.1" })
            {
                AddJob(Job.Default
                    .WithRuntime(CoreRuntime.Core10_0)
                    .WithWarmupCount(3)
                    .WithIterationCount(10)
                    .WithNuGet("AvroConvert", version)
                    .WithId(version));
            }

            SummaryStyle = BenchmarkDotNet.Reports.SummaryStyle.Default.WithRatioStyle(BenchmarkDotNet.Columns.RatioStyle.Trend);
        }
    }

    private User _user;
    private byte[] _userAvro;
    private List<User> _users1k;
    private byte[] _users1kAvro;
    private List<User> _users20k;
    private byte[] _users20kAvro;

    [GlobalSetup]
    public void Setup()
    {
        _user = MakeUser(1);
        _userAvro = AvroConvert.Serialize(_user);

        _users1k = Enumerable.Range(0, 1_000).Select(MakeUser).ToList();
        _users1kAvro = AvroConvert.Serialize(_users1k);

        _users20k = Enumerable.Range(0, 20_000).Select(MakeUser).ToList();
        _users20kAvro = AvroConvert.Serialize(_users20k);
    }

    private static User MakeUser(int i) => new()
    {
        Id = i,
        IsActive = i % 2 == 0,
        Name = "user-" + i,
        Age = 20 + i % 50,
        Contact = new Contact { Id = i, Address = "Street " + i, HouseNumber = i, City = "City", PostCode = "00-000" },
        Offerings = new List<Offering>
        {
            new() { Id = i, ProductNumber = new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), Price = i, Currency = "PLN", Discount = i % 5 == 0 },
            new() { Id = i + 1, ProductNumber = new Guid(i + 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), Price = i * 2, Currency = "EUR", Discount = false }
        }
    };

    [Benchmark] public byte[] Serialize_Single() => AvroConvert.Serialize(_user);
    [Benchmark] public User Deserialize_Single() => AvroConvert.Deserialize<User>(_userAvro);

    [Benchmark] public byte[] Serialize_1k() => AvroConvert.Serialize(_users1k);
    [Benchmark] public List<User> Deserialize_1k() => AvroConvert.Deserialize<List<User>>(_users1kAvro);

    [Benchmark] public byte[] Serialize_20k() => AvroConvert.Serialize(_users20k);
    [Benchmark] public List<User> Deserialize_20k() => AvroConvert.Deserialize<List<User>>(_users20kAvro);
}
