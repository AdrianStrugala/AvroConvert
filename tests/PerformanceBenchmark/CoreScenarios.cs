using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using GrandeBenchmark;
using SolTechnology.Avro;

namespace SolTechnology.PerformanceBenchmark
{
    /// <summary>
    /// Scenarios from docs/PerformanceRewriteAnalysis.md §5 (phase 0 baseline).
    /// Run: dotnet run -c Release --project tests/PerformanceBenchmark -- --filter *CoreScenarios*
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net10_0, warmupCount: 3, iterationCount: 10)]
    public class CoreScenarios
    {
        public class Flat
        {
            public int Id { get; set; }
            public long Ticks { get; set; }
            public double Score { get; set; }
            public bool Active { get; set; }
            public string Name { get; set; }
            public float Ratio { get; set; }
            public int? Optional { get; set; }
            public string Email { get; set; }
        }

        public class Level3 { public int Value { get; set; } public string Tag { get; set; } }
        public class Level2 { public long Key { get; set; } public Level3 Inner { get; set; } }
        public class Level1 { public string Name { get; set; } public Level2 Inner { get; set; } }

        private Flat _flat;
        private byte[] _flatAvro;

        private List<Flat> _flatList;
        private byte[] _flatListAvro;

        private List<int> _intList;
        private byte[] _intListAvro;

        private Dictionary<string, Flat> _dictionary;
        private byte[] _dictionaryAvro;

        private Level1 _nested;
        private byte[] _nestedAvro;

        private List<User> _multiBlock;
        private byte[] _multiBlockAvro;
        private byte[] _multiBlockDeflateAvro;

        [GlobalSetup]
        public void Setup()
        {
            var rnd = new Random(42);
            Flat MakeFlat(int i) => new()
            {
                Id = i,
                Ticks = rnd.NextInt64(),
                Score = rnd.NextDouble(),
                Active = i % 2 == 0,
                Name = "name-" + i,
                Ratio = (float)rnd.NextDouble(),
                Optional = i % 3 == 0 ? null : i,
                Email = $"user{i}@example.com"
            };

            _flat = MakeFlat(1);
            _flatAvro = AvroConvert.Serialize(_flat);

            _flatList = Enumerable.Range(0, 1000).Select(MakeFlat).ToList();
            _flatListAvro = AvroConvert.Serialize(_flatList);

            _intList = Enumerable.Range(0, 10_000).ToList();
            _intListAvro = AvroConvert.Serialize(_intList);

            _dictionary = Enumerable.Range(0, 500).ToDictionary(i => "k" + i, MakeFlat);
            _dictionaryAvro = AvroConvert.Serialize(_dictionary);

            _nested = new Level1 { Name = "root", Inner = new Level2 { Key = 7, Inner = new Level3 { Value = 3, Tag = "leaf" } } };
            _nestedAvro = AvroConvert.Serialize(_nested);

            _multiBlock = Enumerable.Range(0, 20_000).Select(i => new User
            {
                Id = i,
                IsActive = i % 2 == 0,
                Name = "user-" + i,
                Age = 20 + i % 50,
                Contact = new Contact { Id = i, Address = "Street " + i, HouseNumber = i, City = "City", PostCode = "00-000" },
                Offerings = new List<Offering>
                {
                    new() { Id = i, ProductNumber = Guid.NewGuid(), Price = i, Currency = "PLN", Discount = i % 5 == 0 }
                }
            }).ToList();
            _multiBlockAvro = AvroConvert.Serialize(_multiBlock);
            _multiBlockDeflateAvro = AvroConvert.Serialize(_multiBlock, CodecType.Deflate);
        }

        [Benchmark] public byte[] Serialize_Flat() => AvroConvert.Serialize(_flat);
        [Benchmark] public Flat Deserialize_Flat() => AvroConvert.Deserialize<Flat>(_flatAvro);

        [Benchmark] public byte[] Serialize_FlatList1k() => AvroConvert.Serialize(_flatList);
        [Benchmark] public List<Flat> Deserialize_FlatList1k() => AvroConvert.Deserialize<List<Flat>>(_flatListAvro);

        [Benchmark] public byte[] Serialize_IntList10k() => AvroConvert.Serialize(_intList);
        [Benchmark] public List<int> Deserialize_IntList10k() => AvroConvert.Deserialize<List<int>>(_intListAvro);

        [Benchmark] public byte[] Serialize_Dictionary500() => AvroConvert.Serialize(_dictionary);
        [Benchmark] public Dictionary<string, Flat> Deserialize_Dictionary500() => AvroConvert.Deserialize<Dictionary<string, Flat>>(_dictionaryAvro);

        [Benchmark] public byte[] Serialize_Nested3() => AvroConvert.Serialize(_nested);
        [Benchmark] public Level1 Deserialize_Nested3() => AvroConvert.Deserialize<Level1>(_nestedAvro);

        [Benchmark] public byte[] Serialize_MultiBlock20k() => AvroConvert.Serialize(_multiBlock);
        [Benchmark] public List<User> Deserialize_MultiBlock20k() => AvroConvert.Deserialize<List<User>>(_multiBlockAvro);

        [Benchmark] public byte[] Serialize_MultiBlock20k_Deflate() => AvroConvert.Serialize(_multiBlock, CodecType.Deflate);
        [Benchmark] public List<User> Deserialize_MultiBlock20k_Deflate() => AvroConvert.Deserialize<List<User>>(_multiBlockDeflateAvro);
    }
}
