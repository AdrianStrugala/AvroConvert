using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Http;

namespace AvroConvertHttpTests;

public class HttpClientTests
{
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(respond(request));
        }
    }

    [Test]
    public async Task GetFromAvroAsync_sends_avro_accept_header()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = AvroContent.Create(TestServerFactory.SampleOrder()) });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };

        await client.GetFromAvroAsync<Order>("orders");

        handler.LastRequest.Headers.Accept.Should().ContainSingle(a => a.MediaType == AvroMediaTypes.Avro);
    }

    [Test]
    public async Task GetFromAvroAsync_throws_on_error_status()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };

        var act = () => client.GetFromAvroAsync<Order>("orders");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Test]
    public async Task ReadFromAvroAsync_rejects_json_with_a_clear_message()
    {
        var content = new StringContent("{\"id\":1}", Encoding.UTF8, "application/json");

        var act = () => content.ReadFromAvroAsync<Order>();

        await act.Should().ThrowAsync<NotSupportedException>().WithMessage("*application/json*");
    }

    [Test]
    public async Task AvroContent_reports_length_and_media_type()
    {
        var content = AvroContent.Create(TestServerFactory.SampleOrder(), new AvroConvertOptions { Codec = CodecType.Brotli });

        content.Headers.ContentType!.MediaType.Should().Be(AvroMediaTypes.Avro);
        content.Headers.ContentLength.Should().BeGreaterThan(0);
        AvroConvert.Deserialize<Order>(await content.ReadAsByteArrayAsync()).Should().BeEquivalentTo(TestServerFactory.SampleOrder());
    }

#pragma warning disable CS0618
    [Test]
    public async Task Obsolete_names_still_work()
    {
        var handler = new RecordingHandler(r => new HttpResponseMessage(HttpStatusCode.OK) { Content = AvroContent.Create(TestServerFactory.SampleOrder()) });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };

        (await client.GetAsAvro<Order>("orders")).Should().BeEquivalentTo(TestServerFactory.SampleOrder());
        (await client.PostAsAvro("orders", TestServerFactory.SampleOrder())).StatusCode.Should().Be(HttpStatusCode.OK);
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.Should().Be(Consts.AvroHeader);
    }
#pragma warning restore CS0618
}
