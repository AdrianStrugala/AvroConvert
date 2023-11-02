using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Write;

namespace CoreBenchmarks;

[MemoryDiagnoser]
public class WriterBenchmarksDuration
{
    private TimeSpan _duration;
    private WriteResolver _resolver;
    private DurationSchema _schema;
    private IWriter _writer;

    [GlobalSetup]
    public void Setup()
    {
        _duration = TimeSpan.FromDays(37.3d);
        _schema = (DurationSchema)Schema.Create(_duration);
        _writer = new Writer(Stream.Null);
    }

    [Benchmark(Baseline = true)]
    public int Current()
    {
        var duration = _duration;

        var baseSchema = (FixedSchema)_schema.BaseTypeSchema;
        byte[] bytes = new byte[baseSchema.Size];
        var monthsBytes = BitConverter.GetBytes(0);
        var daysBytes = BitConverter.GetBytes(duration.Days);

        var milliseconds = ((duration.Hours * 60 + duration.Minutes) * 60 + duration.Seconds) * 1000 +
                           duration.Milliseconds;
        var millisecondsBytes = BitConverter.GetBytes(milliseconds);


        Array.Copy(monthsBytes, 0, bytes, 0, 4);
        Array.Copy(daysBytes, 0, bytes, 4, 4);
        Array.Copy(millisecondsBytes, 0, bytes, 8, 4);


        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes); //reverse it so we get little endian.

        _writer.WriteFixed(bytes);
        return bytes[0];
    }

    [Benchmark]
    public int Optimized()
    {
        var duration = _duration;

        var baseSchema = (FixedSchema)_schema.BaseTypeSchema;
        Span<byte> buffer = stackalloc byte[baseSchema.Size];
        buffer.Slice(0, 4).Fill(0);

        var days = duration.Days;
        var daysBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref days, 1));

        daysBytes.CopyTo(buffer.Slice(4, 4));

        var milliseconds = ((duration.Hours * 60 + duration.Minutes) * 60 + duration.Seconds) * 1000 +
                           duration.Milliseconds;

        var millisecondsBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref milliseconds, 1));

        millisecondsBytes.CopyTo(buffer.Slice(8, 4));

        if (!BitConverter.IsLittleEndian)
            buffer.Reverse();

        _writer.WriteFixed(buffer);
        return buffer[10];
    }
}