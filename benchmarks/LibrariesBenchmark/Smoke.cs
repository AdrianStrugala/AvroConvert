// Smoke test: `dotnet run -c Release -- smoke` – round-trips every library at 1 / 1k records and prints payload sizes.
using LibrariesBenchmark;
using SolTechnology.Avro;

public static class Smoke
{
    public static void Run()
    {
        var apache = new ApacheAvro();
        foreach (var count in new[] { 1, 1_000 })
        {
            var users = Dataset.Users(count);
            var a = AvroConvert.Serialize(users);
            var b = apache.Serialize(Dataset.SpecificUsers(count));
            var c = NewtonsoftJson.Serialize(users);
            Check(AvroConvert.Deserialize<List<User>>(a), users, "AvroConvert");
            Check(apache.Deserialize(b).Select(Dataset.FromSpecific).ToList(), users, "ApacheAvro");
            Check(NewtonsoftJson.Deserialize(c), users, "NewtonsoftJson");
            Check(AvroConvert.Deserialize<List<User>>(b), users, "AvroConvert reads ApacheAvro");

            // The container headers name the records in different namespaces (LibrariesBenchmark vs the avrogen
            // classes in LibrariesBenchmark.Specific); the binary data itself has no names and must be identical.
            var ours = users.SelectMany(u => AvroConvert.SerializeHeadless(u, Headless)).ToArray();
            var theirs = Dataset.SpecificUsers(count).SelectMany(ApacheHeadless).ToArray();
            if (!ours.AsSpan().SequenceEqual(theirs)) throw new Exception("Binary data differs");
            Console.WriteLine("  ok  same binary data");
            Console.WriteLine($"{count,6} records: AvroConvert {a.Length:N0} B, Apache.Avro {b.Length:N0} B, Newtonsoft.Json {c.Length:N0} B");
        }
    }

    private static readonly AvroSchema Headless = AvroSchema.Parse(Dataset.ApacheSchema);

    private static byte[] ApacheHeadless(LibrariesBenchmark.Specific.User user)
    {
        var stream = new MemoryStream();
        new Avro.Specific.SpecificDatumWriter<LibrariesBenchmark.Specific.User>(LibrariesBenchmark.Specific.User._SCHEMA).Write(user, new Avro.IO.BinaryEncoder(stream));
        return stream.ToArray();
    }

    private static void Check(List<User> actual, List<User> expected, string label)
    {
        if (actual.Count != expected.Count) throw new Exception($"{label}: count {actual.Count} != {expected.Count}");
        for (var i = 0; i < expected.Count; i++)
        {
            var x = actual[i]; var y = expected[i];
            if (x.Id != y.Id || x.Name != y.Name || x.Contact.City != y.Contact.City || x.Offerings.Count != y.Offerings.Count
                || x.Offerings[1].ProductNumber != y.Offerings[1].ProductNumber)
                throw new Exception($"{label}: mismatch at {i}");
        }
        Console.WriteLine($"  ok  {label}");
    }
}
