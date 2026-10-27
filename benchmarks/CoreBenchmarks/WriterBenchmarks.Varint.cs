using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Features.Streaming;

namespace CoreBenchmarks;

/// <summary>
/// Zig-zag varint writing: byte by byte (current Writer.WriteLong) vs encoding into a buffer and writing it once.
/// Values like in the libraries benchmark dataset: ids, ages, string lengths, prices; mostly 1-3 bytes.
/// Run: dotnet run -c Release -- --filter '*WriterBenchmarksVarint*'
/// </summary>
[MemoryDiagnoser]
public class WriterBenchmarksVarint
{
    private readonly long[] _values = Enumerable.Range(0, 1000)
        .SelectMany(i => new long[] { i, i % 50 + 20, 6 + i % 4, 36, i * 2, 3 })
        .ToArray();

    private readonly MemoryStream _memoryStream = new(1 << 20);
    private readonly ArrayBufferWriter<byte> _bufferWriter = new(1 << 20);
    private BufferWriterStream _bufferWriterStream;

    [GlobalSetup]
    public void Setup() => _bufferWriterStream = new BufferWriterStream(_bufferWriter);

    [Benchmark(Baseline = true)]
    public long MemoryStream_Writer()
    {
        _memoryStream.Position = 0;
        var writer = new Writer(_memoryStream);
        foreach (var value in _values)
        {
            writer.WriteLong(value);
        }

        return _memoryStream.Position;
    }

    [Benchmark]
    public long MemoryStream_ByteByByte()
    {
        _memoryStream.Position = 0;
        foreach (var value in _values)
        {
            ByteByByte(_memoryStream, value);
        }

        return _memoryStream.Position;
    }

    [Benchmark]
    public long MemoryStream_Stackalloc()
    {
        _memoryStream.Position = 0;
        foreach (var value in _values)
        {
            Stackalloc(_memoryStream, value);
        }

        return _memoryStream.Position;
    }

    [Benchmark]
    public long MemoryStream_StackallocSkipLocalsInit()
    {
        _memoryStream.Position = 0;
        foreach (var value in _values)
        {
            StackallocSkipLocalsInit(_memoryStream, value);
        }

        return _memoryStream.Position;
    }

    [Benchmark]
    public long MemoryStream_PackedUlong()
    {
        _memoryStream.Position = 0;
        foreach (var value in _values)
        {
            PackedUlong(_memoryStream, value);
        }

        return _memoryStream.Position;
    }

    [Benchmark]
    public long BufferWriter_ByteByByte()
    {
        _bufferWriter.ResetWrittenCount();
        foreach (var value in _values)
        {
            ByteByByte(_bufferWriterStream, value);
        }

        return _bufferWriter.WrittenCount;
    }

    [Benchmark]
    public long BufferWriter_Stackalloc()
    {
        _bufferWriter.ResetWrittenCount();
        foreach (var value in _values)
        {
            Stackalloc(_bufferWriterStream, value);
        }

        return _bufferWriter.WrittenCount;
    }

    private static void ByteByByte(Stream stream, long value)
    {
        ulong n = (ulong)((value << 1) ^ (value >> 63));
        while ((n & ~0x7FUL) != 0)
        {
            stream.WriteByte((byte)((n & 0x7f) | 0x80));
            n >>= 7;
        }

        stream.WriteByte((byte)n);
    }

    private static void Stackalloc(Stream stream, long value)
    {
        ulong n = (ulong)((value << 1) ^ (value >> 63));
        if (n < 0x80)
        {
            stream.WriteByte((byte)n);
            return;
        }

        Span<byte> buffer = stackalloc byte[10];
        var length = 0;
        while ((n & ~0x7FUL) != 0)
        {
            buffer[length++] = (byte)((n & 0x7f) | 0x80);
            n >>= 7;
        }

        buffer[length++] = (byte)n;
        stream.Write(buffer[..length]);
    }

    [SkipLocalsInit]
    private static void StackallocSkipLocalsInit(Stream stream, long value)
    {
        ulong n = (ulong)((value << 1) ^ (value >> 63));
        if (n < 0x80)
        {
            stream.WriteByte((byte)n);
            return;
        }

        Span<byte> buffer = stackalloc byte[10];
        var length = 0;
        while ((n & ~0x7FUL) != 0)
        {
            buffer[length++] = (byte)((n & 0x7f) | 0x80);
            n >>= 7;
        }

        buffer[length++] = (byte)n;
        stream.Write(buffer[..length]);
    }

    // Up to 8 bytes packed into a ulong local and written as one span over it.
    private static void PackedUlong(Stream stream, long value)
    {
        ulong n = (ulong)((value << 1) ^ (value >> 63));
        if (n < 0x80)
        {
            stream.WriteByte((byte)n);
            return;
        }

        if (n >= 1UL << 56)
        {
            ByteByByte(stream, value);
            return;
        }

        ulong packed = 0;
        var length = 0;
        while ((n & ~0x7FUL) != 0)
        {
            packed |= ((n & 0x7f) | 0x80) << (8 * length++);
            n >>= 7;
        }

        packed |= n << (8 * length++);
        stream.Write(MemoryMarshal.AsBytes(new Span<ulong>(ref packed))[..length]);
    }
}
