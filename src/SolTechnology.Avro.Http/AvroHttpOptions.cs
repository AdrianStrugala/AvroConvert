namespace SolTechnology.Avro.Http;

/// <summary>
/// Options for Minimal API endpoints (<see cref="Avro{T}"/> request bodies and <see cref="AvroResult{T}"/> responses).
/// Configure with <c>services.Configure&lt;AvroHttpOptions&gt;(o =&gt; ...)</c>.
/// </summary>
public sealed class AvroHttpOptions
{
    /// <summary>Options passed to AvroConvert; null uses the AvroConvert defaults.</summary>
    public AvroConvertOptions SerializerOptions { get; set; }
}
