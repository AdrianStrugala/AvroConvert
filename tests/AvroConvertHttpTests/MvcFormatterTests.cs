using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Http;

namespace AvroConvertHttpTests;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    [HttpGet]
    public Order Get() => TestServerFactory.SampleOrder();

    [HttpPost]
    public Order Post(Order order) => order;
}

public class MvcFormatterTests
{
    private WebApplication _app;
    private HttpClient _client;

    [SetUp]
    public async Task SetUp()
    {
        (_app, _client) = await TestServerFactory.Start(
            services => services.AddControllers().AddApplicationPart(typeof(OrdersController).Assembly).AddAvroFormatters(),
            app => app.MapControllers());
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Test]
    public async Task Get_with_avro_accept_header_returns_avro()
    {
        var order = await _client.GetFromAvroAsync<Order>("orders");

        order.Should().BeEquivalentTo(TestServerFactory.SampleOrder());
    }

    [Test]
    public async Task Get_without_accept_header_keeps_json_as_default()
    {
        var response = await _client.GetAsync("orders");

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Test]
    public async Task Post_avro_body_round_trips()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "orders") { Content = AvroContent.Create(TestServerFactory.SampleOrder()) };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(AvroMediaTypes.Avro));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(AvroMediaTypes.Avro);
        (await response.Content.ReadFromAvroAsync<Order>()).Should().BeEquivalentTo(TestServerFactory.SampleOrder());
    }

    [Test]
    public async Task Post_invalid_avro_body_returns_400_not_500()
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes("definitely not avro"));
        content.Headers.ContentType = new MediaTypeHeaderValue(AvroMediaTypes.Avro);

        var response = await _client.PostAsync("orders", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Output_formatter_applies_codec_from_options()
    {
        await TearDown();
        (_app, _client) = await TestServerFactory.Start(
            services => services.AddControllers().AddApplicationPart(typeof(OrdersController).Assembly)
                .AddAvroFormatters(new AvroConvertOptions { Codec = CodecType.Deflate }),
            app => app.MapControllers());

        var request = new HttpRequestMessage(HttpMethod.Get, "orders");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(AvroMediaTypes.Avro));
        var bytes = await (await _client.SendAsync(request)).Content.ReadAsByteArrayAsync();

        Encoding.ASCII.GetString(bytes).Should().Contain("deflate");
        AvroConvert.Deserialize<Order>(bytes).Should().BeEquivalentTo(TestServerFactory.SampleOrder());
    }
}
