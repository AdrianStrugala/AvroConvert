using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using SolTechnology.Avro.Codec;

namespace SolTechnology.Avro.Http
{
    public class AvroOutputFormatter : OutputFormatter
    {
        private readonly CodecType _codec;

        public AvroOutputFormatter(Codec.CodecType codec = Codec.CodecType.Null)
        {
            _codec = codec;
            this.SupportedMediaTypes.Clear();

            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/avro"));
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var avroBody = AvroConvert.Serialize(context.Object, _codec);

            var response = context.HttpContext.Response;
            response.ContentLength = avroBody.Length;

            await response.Body.WriteAsync(avroBody);
        }
    }
}