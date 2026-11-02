using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SolTechnology.Avro.Http;

/// <summary>
/// A Minimal API parameter that reads an <c>application/avro</c> request body item by item while the handler enumerates it:
/// <c>app.MapPost("/import", async (AvroStream&lt;Order&gt; orders) =&gt; { await foreach (var order in orders) ... })</c>.
/// Only the current data block is held in memory. A request with another content type gets 415.
/// </summary>
public sealed class AvroStream<T> : IAsyncEnumerable<T>, IEndpointParameterMetadataProvider
{
    private readonly HttpContext _context;
    private readonly AvroConvertOptions _options;

    private AvroStream(HttpContext context, AvroConvertOptions options)
    {
        _context = context;
        _options = options;
    }

    public static ValueTask<AvroStream<T>> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        if (!AvroMediaTypes.IsAvro(context.Request.ContentType))
        {
            throw new BadHttpRequestException(
                $"Expected a request body with Content-Type '{AvroMediaTypes.Avro}'.",
                StatusCodes.Status415UnsupportedMediaType);
        }

        var options = context.RequestServices?.GetService<IOptions<AvroHttpOptions>>()?.Value.SerializerOptions;
        return ValueTask.FromResult(new AvroStream<T>(context, options));
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var token = cancellationToken.CanBeCanceled ? cancellationToken : _context.RequestAborted;
        return AvroConvert.Deserialize<T>(_context.Request.Body, _options, token).GetAsyncEnumerator(token);
    }

    public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
    {
        builder.Metadata.Add(new AvroStreamAcceptsMetadata());
    }

    private sealed class AvroStreamAcceptsMetadata : IAcceptsMetadata
    {
        public IReadOnlyList<string> ContentTypes { get; } = new[] { AvroMediaTypes.Avro };

        public System.Type RequestType => typeof(IAsyncEnumerable<T>);

        public bool IsOptional => false;
    }
}
