using BenchmarkDotNet.Attributes;
using System.Buffers.Binary;

namespace CoreBenchmarks;

[MemoryDiagnoser]
public class WriterBenchmarksDouble
{
    private readonly double _doubleNumber = 123456.789d;

    [Benchmark(Baseline = true)]
    public void WriteDouble_Old()
    {
        var bytes = BitConverter.GetBytes(_doubleNumber);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
    }

    [Benchmark]
    public void WriteDouble_New()
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(buffer, _doubleNumber);

        if (!BitConverter.IsLittleEndian)
        {
            buffer.Reverse();
        }
    }
}