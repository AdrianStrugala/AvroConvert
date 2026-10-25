using System.Text.Json;

namespace SolTechnology.Avro.Kafka;

internal static class AvroSchemaNames
{
    /// <summary>Full name of a named schema (record, enum, fixed), used by the record name strategies; null for other schemas.</summary>
    public static string FullName(AvroSchema schema)
    {
        using var document = JsonDocument.Parse(schema.CanonicalForm);
        var root = document.RootElement;
        return root.ValueKind == JsonValueKind.Object && root.TryGetProperty("name", out var name)
            ? name.GetString()
            : null;
    }
}
