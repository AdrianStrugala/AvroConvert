using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SolTechnology.Avro.Http;

/// <summary>
/// Writes items to the response as an Avro object container while they are enumerated: each data block is sent as soon
/// as it is full, so a large result is never held in memory. Works as a Minimal API result and as the return value of
/// an MVC action. Create it with <see cref="AvroResults.Stream{T}"/>.
/// </summary>
public sealed class AvroStreamResult<T> : IResult, IStatusCodeHttpResult, IContentTypeHttpResult, IEndpointMetadataProvider
{
    private readonly IAsyncEnumerable<T> _items;
    private readonly AvroConvertOptions _options;

    internal AvroStreamResult(IAsyncEnumerable<T> items, AvroConvertOptions options)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _options = options;
    }

    public int? StatusCode => StatusCodes.Status200OK;

    public string ContentType => AvroMediaTypes.Avro;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var options = _options ?? httpContext.RequestServices?.GetService<IOptions<AvroHttpOptions>>()?.Value.SerializerOptions;
        var response = httpContext.Response;
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = AvroMediaTypes.Avro;

        await AvroConvert.Serialize(_items, response.Body, options, httpContext.RequestAborted);
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new AvroStreamProducesMetadata());
    }

    private sealed class AvroStreamProducesMetadata : IProducesResponseTypeMetadata
    {
        public Type Type => typeof(IAsyncEnumerable<T>);

        public int StatusCode => StatusCodes.Status200OK;

        public IEnumerable<string> ContentTypes { get; } = new[] { AvroMediaTypes.Avro };

        public string Description => null;
    }
}
