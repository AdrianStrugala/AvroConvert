using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SolTechnology.Avro.Http
{
    public static class HttpClientAvroExtensions
    {
        public static async Task<HttpResponseMessage> PostAsAvro(this HttpClient httpClient, string requestUri, object content)
        {
            var body = new ByteArrayContent(AvroConvert.Serialize(content));
            body.Headers.ContentType = new MediaTypeHeaderValue("application/avro");
            return await httpClient.PostAsync(requestUri, body);
        }

        public static async Task<T> GetAsAvro<T>(this HttpClient httpClient, string requestUri)
        {
            var response = await httpClient.GetByteArrayAsync(requestUri);
            T result = AvroConvert.Deserialize<T>(response);
            return result;
        }

        public static async Task<HttpResponseMessage> PutAsAvro(this HttpClient httpClient, string requestUri, object content)
        {
            var body = new ByteArrayContent(AvroConvert.Serialize(content));
            body.Headers.ContentType = new MediaTypeHeaderValue("application/avro");
            return await httpClient.PutAsync(requestUri, body);
        }

        public static async Task<HttpResponseMessage> PatchAsAvro(this HttpClient httpClient, string requestUri, object content)
        {
            var body = new ByteArrayContent(AvroConvert.Serialize(content));
            body.Headers.ContentType = new MediaTypeHeaderValue("application/avro");
            return await httpClient.PatchAsync(requestUri, body);
        }
    }
}
