using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SolTechnology.Avro.Http;

/// <summary>
/// HTTP content that writes items to the request as an Avro object container while they are enumerated
/// (chunked transfer), so a large upload is never held in memory.
/// </summary>
public sealed class AvroStreamContent<T> : HttpContent
{
    private readonly IAsyncEnumerable<T> _items;
    private readonly AvroConvertOptions _options;

    public AvroStreamContent(IAsyncEnumerable<T> items, AvroConvertOptions options = null)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _options = options;
        Headers.ContentType = new MediaTypeHeaderValue(AvroMediaTypes.Avro);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) =>
        SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken) =>
        AvroConvert.Serialize(_items, stream, _options, cancellationToken);

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }
}
