namespace SolTechnology.PerformanceBenchmark
{
    class BenchmarkResult
    {
        public long JsonSerializeTime { get; set; }
        public long JsonDeserializeTime { get; set; }
        public int JsonSize { get; set; }


        public long AvroConvertHeadlessSerializeTime { get; set; }
        public long AvroConvertHeadlessDeserializeTime { get; set; }
        public int AvroConvertHeadlessSize { get; set; }


        public long AvroConvertDeflateSerializeTime { get; set; }
        public long AvroConvertDeflateDeserializeTime { get; set; }
        public int AvroConvertDeflateSize { get; set; }


        public long ApacheAvroSerializeTime { get; set; }
        public long ApacheAvroDeserializeTime { get; set; }
        public int ApacheAvroSize { get; set; }


        public long AvroConvertVNextSerializeTime { get; set; }
        public long AvroConvertVNextDeserializeTime { get; set; }
        public int AvroConvertVNextSize { get; set; }
    }
}