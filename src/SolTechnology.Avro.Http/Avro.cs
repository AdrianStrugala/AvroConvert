using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SolTechnology.Avro.Http;

/// <summary>
/// A Minimal API parameter bound from an <c>application/avro</c> request body:
/// <c>app.MapPost("/orders", (Avro&lt;Order&gt; order) =&gt; ...)</c>.
/// A request with another content type gets 415, a body that is not valid Avro gets 400.
/// </summary>
public sealed class Avro<T> : IEndpointParameterMetadataProvider
{
    public Avro(T value)
    {
        Value = value;
    }

    public T Value { get; }

    public static implicit operator T(Avro<T> avro) => avro is null ? default : avro.Value;

    public static async ValueTask<Avro<T>> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var request = context.Request;
        if (!AvroMediaTypes.IsAvro(request.ContentType))
        {
            throw new BadHttpRequestException(
                $"Expected a request body with Content-Type '{AvroMediaTypes.Avro}'.",
                StatusCodes.Status415UnsupportedMediaType);
        }

        using var body = new MemoryStream(request.ContentLength is > 0 and <= int.MaxValue ? (int)request.ContentLength.Value : 0);
        await request.Body.CopyToAsync(body, context.RequestAborted);
        if (body.Length == 0)
        {
            return null;
        }

        var options = context.RequestServices?.GetService<IOptions<AvroHttpOptions>>()?.Value.SerializerOptions;
        try
        {
            var bytes = AvroBody.ToArray(body);
            return new Avro<T>(options == null ? AvroConvert.Deserialize<T>(bytes) : AvroConvert.Deserialize<T>(bytes, options));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new BadHttpRequestException(
                $"The request body is not valid Avro data for {typeof(T).Name}: {ex.Message}",
                StatusCodes.Status400BadRequest,
                ex);
        }
    }

    public static void PopulateMetadata(ParameterInfo parameter, EndpointBuilder builder)
    {
        builder.Metadata.Add(new AvroAcceptsMetadata(typeof(T)));
    }

    private sealed class AvroAcceptsMetadata(Type requestType) : IAcceptsMetadata
    {
        public IReadOnlyList<string> ContentTypes { get; } = new[] { AvroMediaTypes.Avro };

        public Type RequestType { get; } = requestType;

        public bool IsOptional => false;
    }
}
