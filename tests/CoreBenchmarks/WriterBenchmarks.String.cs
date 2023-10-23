using BenchmarkDotNet.Attributes;
using System.Buffers;
using System.Text;

namespace CoreBenchmarks;

[MemoryDiagnoser]
public class WriterBenchmarksString
{
    [Params(128, 256, 512, 1024, 2048, 4096)]
    public int StringLength { get; set; }

    private string _utf8 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _utf8 = new('x', StringLength);
    }

    [Benchmark(Baseline = true)]
    public int WriteString_Old() =>
        Encoding.UTF8.GetBytes(_utf8).Length;

    [Benchmark]
    public int WriteString_New()
    {
        int maxByteCount = Encoding.UTF8.GetMaxByteCount(_utf8.Length);

        if (maxByteCount <= 512)
        {
            Span<byte> buffer = stackalloc byte[maxByteCount];
            int actualByteCount = Encoding.UTF8.GetBytes(_utf8, buffer);
            buffer = buffer.Slice(0, actualByteCount);

            return buffer.Length;
        }
        else
        {
            var rentedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
            int actualByteCount = Encoding.UTF8.GetBytes(_utf8, rentedBuffer);
            Span<byte> buffer = rentedBuffer.AsSpan(0, actualByteCount);
            var actualLength = buffer.Length;
            ArrayPool<byte>.Shared.Return(rentedBuffer);

            return actualLength;
        }
    }
}