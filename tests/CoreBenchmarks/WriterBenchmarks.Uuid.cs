using System.Text;
using BenchmarkDotNet.Attributes;

namespace CoreBenchmarks
{
    [MemoryDiagnoser]
    public class WriterBenchmarksUuid
    {
        // storing as string also shrinks it from 36 to 16 bytes
        private readonly Guid _guid = Guid.NewGuid();

        [Benchmark(Baseline = true)]
        public int WriteUuid_Old() =>
            Encoding.UTF8.GetBytes(_guid.ToString()).Length;

        [Benchmark]
        public int WriteUuid_Bytes() => _guid.ToByteArray().Length;

        [Benchmark]
        public bool WriteUuid_New()
        {
            Span<byte> buffer = stackalloc byte[16];
            return _guid.TryWriteBytes(buffer);
        }
    }
}