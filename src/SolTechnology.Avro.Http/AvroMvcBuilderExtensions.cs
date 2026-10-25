using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace SolTechnology.Avro.Http;

public static class AvroMvcBuilderExtensions
{
    /// <summary>
    /// Adds the Avro input and output formatters after the existing ones, so JSON stays the default and Avro is used for
    /// <c>Content-Type</c> / <c>Accept: application/avro</c> (and for <c>?format=avro</c> with <c>[FormatFilter]</c>).
    /// </summary>
    public static IMvcBuilder AddAvroFormatters(this IMvcBuilder builder, AvroConvertOptions options = null)
    {
        builder.AddMvcOptions(mvc => Configure(mvc, options));
        return builder;
    }

    /// <inheritdoc cref="AddAvroFormatters(IMvcBuilder, AvroConvertOptions)"/>
    public static IMvcCoreBuilder AddAvroFormatters(this IMvcCoreBuilder builder, AvroConvertOptions options = null)
    {
        builder.AddMvcOptions(mvc => Configure(mvc, options));
        return builder;
    }

    private static void Configure(MvcOptions mvc, AvroConvertOptions options)
    {
        mvc.InputFormatters.Add(new AvroInputFormatter(options));
        mvc.OutputFormatters.Add(new AvroOutputFormatter(options));
        mvc.FormatterMappings.SetMediaTypeMappingForFormat("avro", AvroMediaTypes.Avro);
    }
}
