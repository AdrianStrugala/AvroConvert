using System.Numerics;
using System.Text.RegularExpressions;
using Avro;
using Avro.Generic;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using AvroSchema = SolTechnology.Avro.AvroSchema;

namespace AvroConvertInteropTests;

/// <summary>Reference data from apache/avro share/test/data (see ReferenceData/README.md).</summary>
public class ReferenceDataTests
{
    private static string DataPath(string file) => Path.Combine(TestContext.CurrentContext.TestDirectory, "ReferenceData", file);

    public record SchemaTestCase(string Number, string Input, string Canonical, long? Fingerprint)
    {
        public override string ToString() => Number;
    }

    /// <summary>Entries of schema-tests.txt: <c>&lt;&lt;INPUT</c> (single line or heredoc), <c>&lt;&lt;canonical</c>, optional <c>&lt;&lt;fingerprint</c>.</summary>
    public static IEnumerable<TestCaseData> SchemaTests()
    {
        var path = Path.Combine(Path.GetDirectoryName(typeof(ReferenceDataTests).Assembly.Location)!, "ReferenceData", "schema-tests.txt");
        var lines = File.ReadAllLines(path);
        string number = null, input = null, canonical = null;
        long? fingerprint = null;

        IEnumerable<TestCaseData> Flush()
        {
            if (input != null && canonical != null)
            {
                yield return new TestCaseData(new SchemaTestCase(number, input, canonical, fingerprint)).SetArgDisplayNames(number);
            }
            input = canonical = null;
            fingerprint = null;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var header = Regex.Match(line, @"^// (\d{3})\s*$");
            if (header.Success)
            {
                foreach (var c in Flush()) yield return c;
                number = header.Groups[1].Value;
            }
            else if (line == "<<INPUT")
            {
                var body = new List<string>();
                while (lines[++i] != "INPUT") body.Add(lines[i]);
                input = string.Join("\n", body);
            }
            else if (line.StartsWith("<<INPUT "))
            {
                input = line["<<INPUT ".Length..];
            }
            else if (line.StartsWith("<<canonical "))
            {
                canonical = line["<<canonical ".Length..];
            }
            else if (line.StartsWith("<<fingerprint "))
            {
                fingerprint = long.Parse(line["<<fingerprint ".Length..]);
            }
        }

        foreach (var c in Flush()) yield return c;
    }

    [TestCaseSource(nameof(SchemaTests))]
    public void SchemaTests_CanonicalFormAndFingerprint(SchemaTestCase test)
    {
        var schema = AvroSchema.Parse(test.Input);

        schema.CanonicalForm.Should().Be(test.Canonical);
        if (test.Fingerprint is { } expected)
        {
            schema.Fingerprint.Should().Be(expected);
        }
    }

    public class Weather
    {
        public string station { get; set; } = "";
        public long time { get; set; }
        public int temp { get; set; }
    }

    [TestCase("weather.avro")]
    [TestCase("weather-snappy.avro")]
    [TestCase("weather-sorted.avro")]
    public void WeatherFiles_ReadAsRecordsInJsonFile(string file)
    {
        var expected = File.ReadAllLines(DataPath("weather.json"))
            .Select(l => Newtonsoft.Json.JsonConvert.DeserializeObject<Weather>(l)!)
            .ToList();
        var avro = File.ReadAllBytes(DataPath(file));

        var records = AvroConvert.Deserialize<List<Weather>>(avro);

        if (file == "weather-sorted.avro")
        {
            records.Should().BeEquivalentTo(expected);
        }
        else
        {
            records.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }
    }

    [Test]
    public void WeatherFile_JsonEncodingMatchesJsonFile()
    {
        var expected = File.ReadAllLines(DataPath("weather.json"));

        var json = AvroConvert.Avro2JsonEncoding(File.ReadAllBytes(DataPath("weather.avro")));

        json.Split('\n').Should().Equal(expected);
    }

    [Test]
    public void SyncMarkerInMetadata_FileIsReadCompletely()
    {
        var avro = File.ReadAllBytes(DataPath("syncInMeta.avro"));
        var (_, apacheRecords) = ApacheAvro.ReadContainer(avro);

        var lines = AvroConvert.Avro2JsonEncoding(avro).Split('\n');

        lines.Should().HaveCount(apacheRecords.Count);
    }
}

/// <summary>
/// Every logical type at the binary level: the expected encoded value is computed from the specification and Apache.Avro is
/// used only to read/write the underlying type, so the check does not depend on Apache's logical-type conversions.
/// </summary>
public class LogicalTypeMatrixTests
{
    public class Holder<T>
    {
        public T Value { get; set; } = default!;
    }

    public record LogicalCase(string Name, string LogicalType, string RawType, object ClrValue, object RawValue)
    {
        public override string ToString() => Name;
    }

    private static readonly DateTime Instant = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc).AddTicks(1_234_567);
    private static readonly long TicksSinceEpoch = Instant.Ticks - DateTime.UnixEpoch.Ticks;

    private static byte[] TwoComplementBigEndian(BigInteger value, int size)
    {
        var bytes = value.ToByteArray(isUnsigned: false, isBigEndian: true);
        if (size == 0) return bytes;
        var padded = new byte[size];
        Array.Fill(padded, value.Sign < 0 ? (byte)0xFF : (byte)0x00);
        bytes.CopyTo(padded, size - bytes.Length);
        return padded;
    }

    private static byte[] Duration(int months, int days, int milliseconds)
    {
        var bytes = new byte[12];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), months);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), days);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), milliseconds);
        return bytes;
    }

    public static IEnumerable<TestCaseData> Cases()
    {
        var guid = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var date = new DateOnly(2024, 2, 29);
        var time = new TimeOnly(13, 14, 15, 16, 17);
        var local = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Unspecified).AddTicks(1_234_567);
        var localTicks = local.Ticks - DateTime.UnixEpoch.Ticks;

        var cases = new[]
        {
            new LogicalCase("date", @"{""type"":""int"",""logicalType"":""date""}", @"""int""", date, date.DayNumber - DateOnly.FromDateTime(DateTime.UnixEpoch).DayNumber),
            new LogicalCase("time-millis", @"{""type"":""int"",""logicalType"":""time-millis""}", @"""int""", new TimeOnly(13, 14, 15, 16), (int)new TimeOnly(13, 14, 15, 16).ToTimeSpan().TotalMilliseconds),
            new LogicalCase("time-micros", @"{""type"":""long"",""logicalType"":""time-micros""}", @"""long""", time, time.Ticks / 10),
            new LogicalCase("timestamp-millis", @"{""type"":""long"",""logicalType"":""timestamp-millis""}", @"""long""", Instant.AddTicks(-(Instant.Ticks % TimeSpan.TicksPerMillisecond)), TicksSinceEpoch / TimeSpan.TicksPerMillisecond),
            new LogicalCase("timestamp-micros", @"{""type"":""long"",""logicalType"":""timestamp-micros""}", @"""long""", Instant.AddTicks(-(Instant.Ticks % 10)), TicksSinceEpoch / 10),
            new LogicalCase("timestamp-nanos", @"{""type"":""long"",""logicalType"":""timestamp-nanos""}", @"""long""", Instant, TicksSinceEpoch * 100),
            new LogicalCase("timestamp-nanos (long member)", @"{""type"":""long"",""logicalType"":""timestamp-nanos""}", @"""long""", 1_700_000_000_123_456_789L, 1_700_000_000_123_456_789L),
            new LogicalCase("local-timestamp-millis", @"{""type"":""long"",""logicalType"":""local-timestamp-millis""}", @"""long""", local.AddTicks(-(local.Ticks % TimeSpan.TicksPerMillisecond)), localTicks / TimeSpan.TicksPerMillisecond),
            new LogicalCase("local-timestamp-micros", @"{""type"":""long"",""logicalType"":""local-timestamp-micros""}", @"""long""", local.AddTicks(-(local.Ticks % 10)), localTicks / 10),
            new LogicalCase("local-timestamp-nanos", @"{""type"":""long"",""logicalType"":""local-timestamp-nanos""}", @"""long""", local, localTicks * 100),
            new LogicalCase("decimal (bytes)", @"{""type"":""bytes"",""logicalType"":""decimal"",""precision"":10,""scale"":2}", @"""bytes""", -12345.67m, TwoComplementBigEndian(-1234567, 0)),
            new LogicalCase("decimal (fixed)", @"{""type"":""fixed"",""name"":""Money"",""size"":8,""logicalType"":""decimal"",""precision"":12,""scale"":2}", @"{""type"":""fixed"",""name"":""Money"",""size"":8}", -12.5m, TwoComplementBigEndian(-1250, 8)),
            new LogicalCase("big-decimal", @"{""type"":""bytes"",""logicalType"":""big-decimal""}", @"""bytes""", 123.45m, new byte[] { 0x04, 0x30, 0x39, 0x04 }),
            new LogicalCase("uuid (string)", @"{""type"":""string"",""logicalType"":""uuid""}", @"""string""", guid, guid.ToString()),
            new LogicalCase("uuid (fixed)", @"{""type"":""fixed"",""name"":""Id"",""size"":16,""logicalType"":""uuid""}", @"{""type"":""fixed"",""name"":""Id"",""size"":16}", guid, Convert.FromHexString("00112233445566778899aabbccddeeff")),
            new LogicalCase("duration", @"{""type"":""fixed"",""name"":""Dur"",""size"":12,""logicalType"":""duration""}", @"{""type"":""fixed"",""name"":""Dur"",""size"":12}", new TimeSpan(3, 4, 0, 0, 5), Duration(0, 3, 4 * 3_600_000 + 5)),
        };

        return cases.Select(c => new TestCaseData(c).SetArgDisplayNames(c.Name));
    }

    private static string Record(string valueType) => $@"{{""type"":""record"",""name"":""Holder"",""fields"":[{{""name"":""Value"",""type"":{valueType}}}]}}";

    private static object ApacheRaw(object value) => value is GenericFixed f ? f.Value : value;

    [TestCaseSource(nameof(Cases))]
    public void AvroConvertWrites_SpecEncodingReadByApache(LogicalCase test)
    {
        var holder = Activator.CreateInstance(typeof(Holder<>).MakeGenericType(test.ClrValue.GetType()))!;
        holder.GetType().GetProperty("Value")!.SetValue(holder, test.ClrValue);

        var bytes = AvroConvert.SerializeHeadless(holder, Record(test.LogicalType));
        var record = ApacheAvro.ReadBinary(Avro.Schema.Parse(Record(test.RawType)), bytes);

        ApacheRaw(record["Value"]).Should().BeEquivalentTo(test.RawValue, o => o.WithStrictOrdering());
    }

    [TestCaseSource(nameof(Cases))]
    public void ApacheWritesSpecEncoding_AvroConvertReads(LogicalCase test)
    {
        var rawSchema = (RecordSchema)Avro.Schema.Parse(Record(test.RawType));
        var record = new GenericRecord(rawSchema);
        var rawValue = rawSchema["Value"].Schema is FixedSchema fixedSchema
            ? new GenericFixed(fixedSchema, (byte[])test.RawValue)
            : test.RawValue;
        record.Add("Value", rawValue);
        var bytes = ApacheAvro.WriteBinary(rawSchema, record);

        var holderType = typeof(Holder<>).MakeGenericType(test.ClrValue.GetType());
        var read = typeof(AvroConvert).GetMethods()
            .Single(m => m.Name == nameof(AvroConvert.DeserializeHeadless) && m.IsGenericMethod && m.GetParameters().Length == 2
                         && m.GetParameters()[0].ParameterType == typeof(byte[]) && m.GetParameters()[1].ParameterType == typeof(string))
            .MakeGenericMethod(holderType)
            .Invoke(null, new object[] { bytes, Record(test.LogicalType) })!;

        holderType.GetProperty("Value")!.GetValue(read).Should().Be(test.ClrValue);
    }
}
