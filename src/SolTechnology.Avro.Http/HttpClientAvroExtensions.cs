using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SolTechnology.Avro.Http;

/// <summary>Avro counterparts of the System.Net.Http.Json extensions.</summary>
public static class HttpClientAvroExtensions
{
    /// <summary>
    /// Sends a GET request with <c>Accept: application/avro</c> and deserializes the response body to <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="HttpRequestException">The response status code does not indicate success.</exception>
    public static async Task<T> GetFromAvroAsync<T>(this HttpClient client, string requestUri, AvroConvertOptions options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(AvroMediaTypes.Avro));

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromAvroAsync<T>(options, cancellationToken).ConfigureAwait(false);
    }

    public static Task<T> GetFromAvroAsync<T>(this HttpClient client, string requestUri, CancellationToken cancellationToken) =>
        client.GetFromAvroAsync<T>(requestUri, null, cancellationToken);

    /// <summary>Sends a POST request with <paramref name="value"/> serialized to Avro as the body.</summary>
    public static Task<HttpResponseMessage> PostAsAvroAsync<T>(this HttpClient client, string requestUri, T value, AvroConvertOptions options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.PostAsync(requestUri, AvroContent.Create(value, options), cancellationToken);
    }

    public static Task<HttpResponseMessage> PostAsAvroAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken) =>
        client.PostAsAvroAsync(requestUri, value, null, cancellationToken);

    /// <summary>Sends a PUT request with <paramref name="value"/> serialized to Avro as the body.</summary>
    public static Task<HttpResponseMessage> PutAsAvroAsync<T>(this HttpClient client, string requestUri, T value, AvroConvertOptions options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.PutAsync(requestUri, AvroContent.Create(value, options), cancellationToken);
    }

    public static Task<HttpResponseMessage> PutAsAvroAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken) =>
        client.PutAsAvroAsync(requestUri, value, null, cancellationToken);

    /// <summary>Sends a PATCH request with <paramref name="value"/> serialized to Avro as the body.</summary>
    public static Task<HttpResponseMessage> PatchAsAvroAsync<T>(this HttpClient client, string requestUri, T value, AvroConvertOptions options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.PatchAsync(requestUri, AvroContent.Create(value, options), cancellationToken);
    }

    public static Task<HttpResponseMessage> PatchAsAvroAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken) =>
        client.PatchAsAvroAsync(requestUri, value, null, cancellationToken);

    [Obsolete("Use PostAsAvroAsync. PostAsAvro will be removed in the next major version.")]
    public static Task<HttpResponseMessage> PostAsAvro(this HttpClient httpClient, string requestUri, object content) =>
        httpClient.PostAsAvroAsync(requestUri, content);

    [Obsolete("Use GetFromAvroAsync, which also sends Accept: application/avro. GetAsAvro will be removed in the next major version.")]
    public static Task<T> GetAsAvro<T>(this HttpClient httpClient, string requestUri) =>
        httpClient.GetFromAvroAsync<T>(requestUri);

    [Obsolete("Use PutAsAvroAsync. PutAsAvro will be removed in the next major version.")]
    public static Task<HttpResponseMessage> PutAsAvro(this HttpClient httpClient, string requestUri, object content) =>
        httpClient.PutAsAvroAsync(requestUri, content);

    [Obsolete("Use PatchAsAvroAsync. PatchAsAvro will be removed in the next major version.")]
    public static Task<HttpResponseMessage> PatchAsAvro(this HttpClient httpClient, string requestUri, object content) =>
        httpClient.PatchAsAvroAsync(requestUri, content);
}
