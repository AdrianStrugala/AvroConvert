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

            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/avro"));
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                context.HttpContext.Request.Body.CopyTo(ms);
                var type = context.ModelType;

                object result = AvroConvert.Deserialize(ms.ToArray(), type);
                return InputFormatterResult.SuccessAsync(result);
            }
        }
    }
}