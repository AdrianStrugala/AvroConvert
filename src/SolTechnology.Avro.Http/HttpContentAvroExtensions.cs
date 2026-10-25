using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SolTechnology.Avro.Http;

public static class HttpContentAvroExtensions
{
    /// <summary>
    /// Reads the content as an Avro object container and deserializes it to <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">The content type is neither <c>application/avro</c> nor <c>application/octet-stream</c>.</exception>
    public static async Task<T> ReadFromAvroAsync<T>(this HttpContent content, AvroConvertOptions options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var mediaType = content.Headers.ContentType?.MediaType;
        if (mediaType != null
            && !string.Equals(mediaType, AvroMediaTypes.Avro, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mediaType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"The content type '{mediaType}' is not '{AvroMediaTypes.Avro}'.");
        }

        var bytes = await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        return options == null ? AvroConvert.Deserialize<T>(bytes) : AvroConvert.Deserialize<T>(bytes, options);
    }

    public static Task<T> ReadFromAvroAsync<T>(this HttpContent content, CancellationToken cancellationToken) =>
        content.ReadFromAvroAsync<T>(null, cancellationToken);
}
