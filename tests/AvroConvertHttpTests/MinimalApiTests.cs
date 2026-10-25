using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Avro.Http;
using System.Linq;

namespace AvroConvertHttpTests;

public class MinimalApiTests
{
    private WebApplication _app;
    private HttpClient _client;

    [SetUp]
    public async Task SetUp()
    {
        (_app, _client) = await TestServerFactory.StartKestrel(
            _ => { },
            app =>
            {
                app.MapGet("/orders/1", () => AvroResults.Avro(TestServerFactory.SampleOrder()));
                app.MapPost("/orders", (Avro<Order> order) => Results.Extensions.Avro(order.Value, statusCode: StatusCodes.Status201Created));
            });
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Test]
    public async Task Avro_result_is_readable_by_the_client()
    {
        var order = await _client.GetFromAvroAsync<Order>("orders/1");

        order.Should().BeEquivalentTo(TestServerFactory.SampleOrder());
    }

    [Test]
    public async Task Avro_body_is_bound_and_status_code_is_kept()
    {
        var response = await _client.PostAsAvroAsync("orders", TestServerFactory.SampleOrder());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadFromAvroAsync<Order>()).Should().BeEquivalentTo(TestServerFactory.SampleOrder());
    }

    [Test]
    public async Task Json_body_on_avro_endpoint_returns_415()
    {
        var response = await _client.PostAsJsonAsync("orders", TestServerFactory.SampleOrder());

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task Invalid_avro_body_returns_400()
    {
        var content = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(AvroMediaTypes.Avro);

        var response = await _client.PostAsync("orders", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public void Endpoints_describe_avro_in_their_metadata()
    {
        var endpoints = ((IEndpointRouteBuilder)_app).DataSources.SelectMany(s => s.Endpoints).ToList();

        endpoints.SelectMany(e => e.Metadata.OfType<IProducesResponseTypeMetadata>())
            .Should().Contain(m => m.ContentTypes.Contains(AvroMediaTypes.Avro) && m.Type == typeof(Order));
        endpoints.SelectMany(e => e.Metadata.OfType<IAcceptsMetadata>())
            .Should().Contain(m => m.ContentTypes.Contains(AvroMediaTypes.Avro) && m.RequestType == typeof(Order));
    }
}
