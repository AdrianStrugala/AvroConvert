namespace AvroConvertTests.Benchmark
{
    class BenchmarkResult
    {
        public string Name { get; set; }
        public long SerializeTime { get; set; }
        public long DeserializeTime { get; set; }
        public int Size { get; set; }

        public int Size2 { get; set; }

        public long SerializeTime2 { get; set; }
        public long DeserializeTime2 { get; set; }

        public long SerializeTime3 { get; set; }
        public long DeserializeTime3 { get; set; }
        public int Size3 { get; set; }
    }
}