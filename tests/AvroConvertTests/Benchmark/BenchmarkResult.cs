namespace AvroConvertTests.Benchmark
{
    class BenchmarkResult
    {
        public string Name { get; set; }
        public long SerializeTime { get; set; }
        public long DeserializeTime { get; set; }
        public int Size { get; set; }
    }
}