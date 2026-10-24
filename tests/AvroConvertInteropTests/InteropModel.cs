namespace AvroConvertInteropTests;

public enum InteropColor
{
    Red,
    Green,
    Blue
}

public class InteropChild
{
    public string Name { get; set; } = "";
    public long Amount { get; set; }
}

public class InteropRecord
{
    public int IntValue { get; set; }
    public long LongValue { get; set; }
    public float FloatValue { get; set; }
    public double DoubleValue { get; set; }
    public bool BoolValue { get; set; }
    public string StringValue { get; set; } = "";
    public byte[] BytesValue { get; set; } = [];
    public List<int> IntList { get; set; } = [];
    public Dictionary<string, string> StringMap { get; set; } = new();
    public InteropChild Child { get; set; } = new();
    public InteropColor Color { get; set; }
    public int? NullableInt { get; set; }
    public decimal DecimalValue { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }

    public static InteropRecord Sample() => new()
    {
        IntValue = -123456,
        LongValue = 9_007_199_254_740_993L,
        FloatValue = 3.5f,
        DoubleValue = -2.25e100,
        BoolValue = true,
        StringValue = "zażółć gęślą jaźń 🚀",
        BytesValue = [0x00, 0x01, 0xFE, 0xFF],
        IntList = [1, -2, 3],
        StringMap = new Dictionary<string, string> { ["a"] = "x", ["b"] = "y" },
        Child = new InteropChild { Name = "child", Amount = 42 },
        Color = InteropColor.Green,
        NullableInt = null,
        DecimalValue = 123456.78901234m,
        Timestamp = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc).AddTicks(1230),
        Id = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
        Date = new DateOnly(2024, 2, 29),
        Time = new TimeOnly(13, 14, 15, 16, 17)
    };
}
