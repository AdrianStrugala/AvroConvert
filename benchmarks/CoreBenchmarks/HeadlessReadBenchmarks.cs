using BenchmarkDotNet.Attributes;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Read.Typed;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace CoreBenchmarks;

/// <summary>
/// Where the time goes when reading one record without a container (the Kafka message case).
/// Run: dotnet run -c Release -- --filter '*HeadlessReadBenchmarks*'
/// </summary>
[MemoryDiagnoser]
public class HeadlessReadBenchmarks
{
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

    private AvroSchema _schema;
    private AvroSchema _parsedSchema;
    private TypeSchema _readerSchema;
    private byte[] _bytes;
    private ReadPlan<User> _plan;
    private ReadPlan<User> _resolvingPlan;

    [GlobalSetup]
    public void Setup()
    {
        AvroConvert.License = AvroLicense.NonCommercial;
        _schema = AvroSchema.Create<User>();
        var user = new User
        {
            Id = 0, IsActive = true, Name = "user-0", Age = 20,
            Contact = new Contact { Id = 0, Address = "Street 0", HouseNumber = 0, City = "City", PostCode = "00-000" },
            Offerings =
            [
                new Offering { Id = 0, ProductNumber = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString(), Price = 0, Currency = "PLN", Discount = true },
                new Offering { Id = 1, ProductNumber = new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString(), Price = 0, Currency = "EUR", Discount = false }
            ]
        };
        _bytes = AvroConvert.SerializeHeadless(user, _schema);
        _readerSchema = Schema.Create(typeof(User));
        _plan = ReadPlanCache.Get<User>(_schema.TypeSchema, _readerSchema, null);
        // The usual Kafka case: the writer schema comes from JSON (registry), the reader schema from the type.
        _parsedSchema = AvroSchema.Parse(_schema.Json);
        _resolvingPlan = ReadPlanCache.Get<User>(_parsedSchema.TypeSchema, _readerSchema, null);
    }

    [Benchmark]
    public User DeserializeHeadless_ParsedWriterSchema() => AvroConvert.DeserializeHeadless<User>(_bytes, _parsedSchema);

    [Benchmark]
    public User PlanOnly_ParsedWriterSchema() => _resolvingPlan.One(new Reader(_bytes));

    [Benchmark(Baseline = true)]
    public User DeserializeHeadless() => AvroConvert.DeserializeHeadless<User>(_bytes, _schema);

    [Benchmark]
    public User DeserializeHeadless_Span() => AvroConvert.DeserializeHeadless<User>(_bytes.AsSpan(), _schema, _schema);

    [Benchmark]
    public object ReaderSchemaLookup() => Schema.Create(typeof(User));

    [Benchmark]
    public object PlanLookup() => ReadPlanCache.Get<User>(_schema.TypeSchema, _readerSchema, null);

    [Benchmark]
    public User PlanOnly() => _plan.One(new Reader(_bytes));
}
