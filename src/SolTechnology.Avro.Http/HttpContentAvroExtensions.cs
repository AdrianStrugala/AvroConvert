using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SolTechnology.Avro.Http;

public static class HttpContentAvroExtensions
{
    private static void EnsureAvro(HttpContent content)
    {
        var mediaType = content.Headers.ContentType?.MediaType;
        if (mediaType != null
            && !string.Equals(mediaType, AvroMediaTypes.Avro, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mediaType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"The content type '{mediaType}' is not '{AvroMediaTypes.Avro}'.");
        }
    }

    /// <summary>
    /// Reads the content as an Avro object container and deserializes it to <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">The content type is neither <c>application/avro</c> nor <c>application/octet-stream</c>.</exception>
    public static async Task<T> ReadFromAvroAsync<T>(this HttpContent content, AvroConvertOptions options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        EnsureAvro(content);

        var bytes = await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        return options == null ? AvroConvert.Deserialize<T>(bytes) : AvroConvert.Deserialize<T>(bytes, options);
    }

    public static Task<T> ReadFromAvroAsync<T>(this HttpContent content, CancellationToken cancellationToken) =>
        content.ReadFromAvroAsync<T>(null, cancellationToken);

    /// <summary>
    /// Reads the content as an Avro object container item by item, while the sequence is enumerated; only the current data
    /// block is held in memory. Use with <see cref="HttpCompletionOption.ResponseHeadersRead"/> to start before the body has arrived.
    /// </summary>
    public static async IAsyncEnumerable<T> ReadFromAvroAsAsyncEnumerable<T>(this HttpContent content, AvroConvertOptions options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        EnsureAvro(content);

        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await foreach (var item in AvroConvert.Deserialize<T>(stream, options, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}
