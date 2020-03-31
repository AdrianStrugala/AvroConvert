using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace SolTechnology.Avro.Http
{
    public class AvroOutputFormatter : OutputFormatter
    {
        public AvroOutputFormatter()
        {
            this.SupportedMediaTypes.Clear();

            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/avro"));
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var avroBody = AvroConvert.Serialize(context.Object);

            var response = context.HttpContext.Response;
            response.ContentLength = avroBody.Length;

            await response.Body.WriteAsync(avroBody);
        }
    }
}