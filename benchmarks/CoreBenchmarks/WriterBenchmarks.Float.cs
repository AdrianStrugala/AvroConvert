using BenchmarkDotNet.Attributes;
using System.Buffers.Binary;

namespace CoreBenchmarks;

[MemoryDiagnoser]
public class WriterBenchmarksFloat
{
    private readonly float _floatNumber = 123456.789f;

    [Benchmark(Baseline = true)]
    public void WriteFloat_Old()
    {
        byte[] buffer = BitConverter.GetBytes(_floatNumber);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(buffer);
        }
    }

    [Benchmark]
    public void WriteFloat_New()
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(buffer, _floatNumber);

        if (!BitConverter.IsLittleEndian)
        {
            buffer.Reverse();
        }
    }
}