using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace SolTechnology.Avro.Http;

/// <summary>Reads <c>application/avro</c> request bodies in MVC controllers.</summary>
public class AvroInputFormatter : InputFormatter
{
    private readonly AvroConvertOptions _options;

    public AvroInputFormatter() : this(null)
    {
    }

    public AvroInputFormatter(AvroConvertOptions options)
    {
        _options = options;
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(AvroMediaTypes.Avro));
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
    {
        var httpContext = context.HttpContext;
        var contentLength = httpContext.Request.ContentLength;
        using var body = new MemoryStream(contentLength is > 0 and <= int.MaxValue ? (int)contentLength.Value : 0);
        await httpContext.Request.Body.CopyToAsync(body, httpContext.RequestAborted);

        if (body.Length == 0)
        {
            return context.TreatEmptyInputAsDefaultValue
                ? await InputFormatterResult.SuccessAsync(GetDefaultValueForType(context.ModelType))
                : await InputFormatterResult.NoValueAsync();
        }

        try
        {
            var model = AvroConvert.Deserialize(AvroBody.ToArray(body), context.ModelType, _options);
            return await InputFormatterResult.SuccessAsync(model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            context.ModelState.TryAddModelError(
                context.ModelName,
                new InputFormatterException($"The request body is not valid Avro data for {context.ModelType.Name}: {ex.Message}", ex),
                context.Metadata);
            return await InputFormatterResult.FailureAsync();
        }
    }
}
