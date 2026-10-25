using System.Collections.Generic;

namespace AvroConvertKafkaTests;

public class Order
{
    public int Id { get; set; }
    public string Customer { get; set; }
    public double Total { get; set; }
    public bool Paid { get; set; }
    public List<string> Tags { get; set; }
}

/// <summary>The Java side's model of an order, with a field the .NET <see cref="Order"/> does not have.</summary>
public class JavaOrder : Order
{
    public string Channel { get; set; }
}

public class OrderKey
{
    public long Id { get; set; }
}

internal static class Samples
{
    public static Order Order() => new()
    {
        Id = 7,
        Customer = "Sol Technology",
        Total = 199.5,
        Paid = true,
        Tags = new List<string> { "licence", "business" }
    };

    /// <summary>A newer version of the schema of <see cref="Order"/>: one more optional field.</summary>
    public const string OrderSchemaV2 = """
        {
          "type": "record",
          "name": "Order",
          "namespace": "AvroConvertKafkaTests",
          "fields": [
            { "name": "Id", "type": "int" },
            { "name": "Customer", "type": "string" },
            { "name": "Total", "type": "double" },
            { "name": "Paid", "type": "boolean" },
            { "name": "Tags", "type": { "type": "array", "items": "string" } },
            { "name": "Channel", "type": ["null", "string"], "default": null }
          ]
        }
        """;

    /// <summary>The same record as a Java producer would declare it: namespaced and with a field the .NET model does not have.</summary>
    public const string JavaOrderSchema = """
        {
          "type": "record",
          "name": "Order",
          "namespace": "com.example.orders",
          "doc": "An order, as declared by a Java service",
          "fields": [
            { "name": "Id", "type": "int" },
            { "name": "Customer", "type": "string", "doc": "Customer name, with spaces" },
            { "name": "Total", "type": "double" },
            { "name": "Paid", "type": "boolean" },
            { "name": "Tags", "type": { "type": "array", "items": "string" } },
            { "name": "Channel", "type": "string", "default": "web shop" }
          ]
        }
        """;
}
