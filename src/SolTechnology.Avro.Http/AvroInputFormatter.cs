using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace SolTechnology.Avro.Http
{
    public class AvroInputFormatter : InputFormatter
    {
        public AvroInputFormatter()
        {
            this.SupportedMediaTypes.Clear();

            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(Consts.AvroHeader));
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            await using MemoryStream ms = new MemoryStream();
            await context.HttpContext.Request.Body.CopyToAsync(ms);
            var type = context.ModelType;

            object result = AvroConvert.Deserialize(ms.ToArray(), type);
            return await InputFormatterResult.SuccessAsync(result);
        }
    }
}