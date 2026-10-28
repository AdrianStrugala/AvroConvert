using BenchmarkDotNet.Attributes;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Read.Typed;
using SolTechnology.Avro.AvroObjectServices.Write;

namespace CoreBenchmarks;

/// <summary>
/// End-to-end read and write through the public API, used to measure each optimisation step on its own.
/// Run: dotnet run -c Release -- --filter '*PerfStepBenchmarks*'
/// </summary>
[MemoryDiagnoser]
public class PerfStepBenchmarks
{
    public class Contact
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public long HouseNumber { get; set; }
        public string City { get; set; }
        public string PostCode { get; set; }
    }

    public class Offering
    {
        public int Id { get; set; }
        public string ProductNumber { get; set; }
        public int Price { get; set; }
        public double Rating { get; set; }
        public string Currency { get; set; }
        public bool Discount { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public long Created { get; set; }
        public Contact Contact { get; set; }
        public List<Offering> Offerings { get; set; }
    }

    private AvroSchema _schema;
    private User _user;
    private List<User> _users;
    private byte[] _userBytes;
    private byte[] _usersFile;
    private ReadPlan<User> _plan;

    [GlobalSetup]
    public void Setup()
    {
        AvroConvert.License = AvroLicense.NonCommercial;
        _schema = AvroSchema.Create<User>();
        _users = Enumerable.Range(0, 1000).Select(Create).ToList();
        _user = _users[42];
        _userBytes = AvroConvert.SerializeHeadless(_user, _schema);
        _usersFile = AvroConvert.Serialize(_users);
        _plan = ReadPlanCache.Get<User>(_schema.TypeSchema, Schema.Create(typeof(User)), null);

        if (!Read_One_Handwritten().Offerings.Select(o => o.ProductNumber).SequenceEqual(_user.Offerings.Select(o => o.ProductNumber))
            || Read_One_Floor().Offerings[^1].Discount != _user.Offerings[^1].Discount
            || !Write_One_Handwritten().AsSpan().SequenceEqual(_userBytes)
            || !Write_One_Floor().AsSpan().SequenceEqual(_userBytes))
        {
            throw new InvalidOperationException("Handwritten benchmark code does not match AvroConvert output");
        }
    }

    private static User Create(int i) => new()
    {
        Id = i,
        IsActive = i % 2 == 0,
        Name = $"user-{i}",
        Age = 18 + i % 60,
        Created = 1_700_000_000_000 + i * 1000L,
        Contact = new Contact { Id = i, Address = $"Street {i}", HouseNumber = i * 7, City = "Warsaw", PostCode = "00-950" },
        Offerings = Enumerable.Range(0, 1 + i % 4).Select(j => new Offering
        {
            Id = i * 10 + j,
            ProductNumber = new Guid(i, (short)j, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString(),
            Price = 100 + j * 250,
            Rating = 3.5 + j * 0.25,
            Currency = j % 2 == 0 ? "PLN" : "EUR",
            Discount = j % 3 == 0
        }).ToList()
    };

    [Benchmark]
    public User Read_One() => AvroConvert.DeserializeHeadless<User>(_userBytes, _schema);

    [Benchmark]
    public User Read_One_PlanOnly() => _plan.One(new Reader(_userBytes));

    /// <summary>What an ideal code generator would emit for this model, on the same Reader.</summary>
    [Benchmark]
    public User Read_One_Handwritten()
    {
        var r = new Reader(_userBytes);
        var u = new User
        {
            Id = r.ReadInt(),
            IsActive = r.ReadBoolean(),
            Name = r.ReadString(),
            Age = r.ReadInt(),
            Created = r.ReadLong(),
            Contact = new Contact { Id = r.ReadInt(), Address = r.ReadString(), HouseNumber = r.ReadLong(), City = r.ReadString(), PostCode = r.ReadString() }
        };
        long n = r.ReadArrayStart();
        var list = new List<Offering>((int)n);
        for (; n != 0; n = r.ReadArrayNext())
        {
            for (long i = 0; i < n; i++)
            {
                list.Add(new Offering { Id = r.ReadInt(), ProductNumber = r.ReadString(), Price = r.ReadInt(), Rating = r.ReadDouble(), Currency = r.ReadString(), Discount = r.ReadBoolean() });
            }
        }
        u.Offerings = list;
        return u;
    }

    [Benchmark]
    public List<User> Read_1000() => AvroConvert.Deserialize<List<User>>(_usersFile);

    /// <summary>Floor for reading: straight from the span, no Reader object, same objects and strings allocated.</summary>
    [Benchmark]
    public User Read_One_Floor()
    {
        ReadOnlySpan<byte> s = _userBytes;
        int p = 0;
        var u = new User { Id = (int)Long(s, ref p), IsActive = s[p++] == 1, Name = Str(s, ref p), Age = (int)Long(s, ref p), Created = Long(s, ref p) };
        u.Contact = new Contact { Id = (int)Long(s, ref p), Address = Str(s, ref p), HouseNumber = Long(s, ref p), City = Str(s, ref p), PostCode = Str(s, ref p) };
        long n = Long(s, ref p);
        var list = new List<Offering>((int)n);
        for (; n != 0; n = Long(s, ref p))
        {
            for (long i = 0; i < n; i++)
            {
                var o = new Offering { Id = (int)Long(s, ref p), ProductNumber = Str(s, ref p), Price = (int)Long(s, ref p) };
                o.Rating = System.Buffers.Binary.BinaryPrimitives.ReadDoubleLittleEndian(s.Slice(p)); p += 8;
                o.Currency = Str(s, ref p);
                o.Discount = s[p++] == 1;
                list.Add(o);
            }
        }
        u.Offerings = list;
        return u;

        static long Long(ReadOnlySpan<byte> s, ref int p)
        {
            ulong b = s[p++];
            ulong n = b & 0x7F;
            int shift = 7;
            while ((b & 0x80) != 0) { b = s[p++]; n |= (b & 0x7F) << shift; shift += 7; }
            return (long)(n >> 1) ^ -(long)(n & 1);
        }

        static string Str(ReadOnlySpan<byte> s, ref int p)
        {
            int len = (int)Long(s, ref p);
            var result = System.Text.Encoding.UTF8.GetString(s.Slice(p, len));
            p += len;
            return result;
        }
    }

    [Benchmark]
    public byte[] Write_One() => AvroConvert.SerializeHeadless(_user, _schema);

    /// <summary>Ideal generated code on the same Writer (memory mode).</summary>
    [Benchmark]
    public byte[] Write_One_Handwritten()
    {
        using var w = new Writer();
        var u = _user;
        w.WriteInt(u.Id); w.WriteBoolean(u.IsActive); w.WriteString(u.Name); w.WriteInt(u.Age); w.WriteLong(u.Created);
        var c = u.Contact;
        w.WriteInt(c.Id); w.WriteString(c.Address); w.WriteLong(c.HouseNumber); w.WriteString(c.City); w.WriteString(c.PostCode);
        w.WriteItemCount(u.Offerings.Count);
        foreach (var o in u.Offerings)
        {
            w.WriteInt(o.Id); w.WriteString(o.ProductNumber); w.WriteInt(o.Price); w.WriteDouble(o.Rating); w.WriteString(o.Currency); w.WriteBoolean(o.Discount);
        }
        w.WriteArrayEnd();
        return w.ToArray();
    }

    /// <summary>Floor for writing: straight into a pooled byte[], no Stream.</summary>
    [Benchmark]
    public byte[] Write_One_Floor()
    {
        var buf = System.Buffers.ArrayPool<byte>.Shared.Rent(4096);
        int p = 0;
        var u = _user;
        Long(buf, ref p, u.Id); buf[p++] = (byte)(u.IsActive ? 1 : 0); Str(buf, ref p, u.Name); Long(buf, ref p, u.Age); Long(buf, ref p, u.Created);
        var c = u.Contact;
        Long(buf, ref p, c.Id); Str(buf, ref p, c.Address); Long(buf, ref p, c.HouseNumber); Str(buf, ref p, c.City); Str(buf, ref p, c.PostCode);
        Long(buf, ref p, u.Offerings.Count);
        foreach (var o in u.Offerings)
        {
            Long(buf, ref p, o.Id); Str(buf, ref p, o.ProductNumber); Long(buf, ref p, o.Price);
            System.Buffers.Binary.BinaryPrimitives.WriteDoubleLittleEndian(buf.AsSpan(p), o.Rating); p += 8;
            Str(buf, ref p, o.Currency); buf[p++] = (byte)(o.Discount ? 1 : 0);
        }
        buf[p++] = 0;
        var result = buf.AsSpan(0, p).ToArray();
        System.Buffers.ArrayPool<byte>.Shared.Return(buf);
        return result;

        static void Long(byte[] b, ref int p, long value)
        {
            ulong n = (ulong)((value << 1) ^ (value >> 63));
            while (n >= 0x80) { b[p++] = (byte)(n | 0x80); n >>= 7; }
            b[p++] = (byte)n;
        }

        static void Str(byte[] b, ref int p, string s)
        {
            if (s.Length < 43)
            {
                // Max 128 bytes after UTF-8, so the length prefix is one byte: write the text after it, then the length.
                int len = System.Text.Encoding.UTF8.GetBytes(s, b.AsSpan(p + 1));
                b[p] = (byte)(len << 1);
                p += 1 + len;
                return;
            }
            int count = System.Text.Encoding.UTF8.GetByteCount(s);
            Long(b, ref p, count);
            p += System.Text.Encoding.UTF8.GetBytes(s, b.AsSpan(p));
        }
    }

    [Benchmark]
    public byte[] Write_1000() => AvroConvert.Serialize(_users);
}
