namespace SolTechnology.Avro.Http;

/// <summary>Media types used for Avro payloads over HTTP.</summary>
public static class AvroMediaTypes
{
    /// <summary><c>application/avro</c> – an Avro object container (header with schema, then the data).</summary>
    public const string Avro = "application/avro";

    internal static bool IsAvro(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return false;
        }

        var separator = contentType.IndexOf(';');
        var mediaType = (separator < 0 ? contentType : contentType[..separator]).Trim();
        return string.Equals(mediaType, Avro, System.StringComparison.OrdinalIgnoreCase);
    }
}
