using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace SolTechnology.Avro.Http;

/// <summary>Writes MVC responses as <c>application/avro</c> when the client asks for it.</summary>
public class AvroOutputFormatter : OutputFormatter
{
    private readonly AvroConvertOptions _options;

    public AvroOutputFormatter() : this((AvroConvertOptions)null)
    {
    }

    public AvroOutputFormatter(CodecType codec) : this(new AvroConvertOptions { Codec = codec })
    {
    }

    public AvroOutputFormatter(AvroConvertOptions options)
    {
        _options = options;
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(AvroMediaTypes.Avro));
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        var httpContext = context.HttpContext;
        AvroConvert.Serialize(context.Object, httpContext.Response.BodyWriter, _options);
        await httpContext.Response.BodyWriter.FlushAsync(httpContext.RequestAborted);
    }
}
