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
/// A Minimal API result that writes <see cref="Value"/> as an <c>application/avro</c> object container.
/// Create it with <c>Results.Extensions.Avro(value)</c> or <see cref="AvroResults.Avro{T}"/>.
/// </summary>
public sealed class AvroResult<T> : IResult, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<T>, IContentTypeHttpResult, IEndpointMetadataProvider
{
    private readonly AvroConvertOptions _options;

    internal AvroResult(T value, AvroConvertOptions options, int? statusCode)
    {
        Value = value;
        _options = options;
        StatusCode = statusCode;
    }

    public T Value { get; }

    object IValueHttpResult.Value => Value;

    public int? StatusCode { get; }

    public string ContentType => AvroMediaTypes.Avro;

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var options = _options ?? httpContext.RequestServices?.GetService<IOptions<AvroHttpOptions>>()?.Value.SerializerOptions;
        var response = httpContext.Response;
        response.StatusCode = StatusCode ?? StatusCodes.Status200OK;
        response.ContentType = AvroMediaTypes.Avro;

        AvroConvert.Serialize(Value, response.BodyWriter, options);
        await response.BodyWriter.FlushAsync(httpContext.RequestAborted);
    }

    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        builder.Metadata.Add(new AvroProducesMetadata(typeof(T)));
    }

    private sealed class AvroProducesMetadata(Type type) : IProducesResponseTypeMetadata
    {
        public Type Type { get; } = type;

        public int StatusCode => StatusCodes.Status200OK;

        public IEnumerable<string> ContentTypes { get; } = new[] { AvroMediaTypes.Avro };

        public string Description => null;
    }
}

/// <summary>Factory methods for <see cref="AvroResult{T}"/>.</summary>
public static class AvroResults
{
    /// <summary>Writes <paramref name="value"/> as <c>application/avro</c>.</summary>
    /// <param name="options">AvroConvert options; null falls back to <see cref="AvroHttpOptions"/>, then to the defaults.</param>
    /// <param name="statusCode">Response status code; 200 when null.</param>
    public static AvroResult<T> Avro<T>(T value, AvroConvertOptions options = null, int? statusCode = null) =>
        new(value, options, statusCode);

    /// <inheritdoc cref="Avro{T}(T, AvroConvertOptions, int?)"/>
    public static AvroResult<T> Avro<T>(this IResultExtensions resultExtensions, T value, AvroConvertOptions options = null, int? statusCode = null)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);
        return new(value, options, statusCode);
    }
}
