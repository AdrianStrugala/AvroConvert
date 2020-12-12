namespace SolTechnology.PerformanceBenchmark
{
    class BenchmarkResult
    {
        public long ApacheAvroSerializeTime { get; set; }
        public long ApacheAvroDeserializeTime { get; set; }
        public int ApacheAvroSize { get; set; }

        public long AvroConvertHeadlessSerializeTime { get; set; }
        public long AvroConvertHeadlessDeserializeTime { get; set; }
        public int AvroConvertHeadlessSize { get; set; }


        public long AvroConvertGzipSerializeTime { get; set; }
        public long AvroConvertGzipDeserializeTime { get; set; }
        public int AvroConvertGzipSize { get; set; }


        public long AvroConvertVNextHeadlessSerializeTime { get; set; }
        public long AvroConvertVNextHeadlessDeserializeTime { get; set; }
        public int AvroConvertVNextSize { get; set; }


        public long AvroConvertVNextGzipSerializeTime { get; set; }
        public long AvroConvertVNextGzipDeserializeTime { get; set; }
        public int AvroConvertVNextGzipSize { get; set; }
    }
}