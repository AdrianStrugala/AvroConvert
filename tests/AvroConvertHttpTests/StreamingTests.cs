using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SolTechnology.Avro.Http;

namespace AvroConvertHttpTests;

[ApiController]
[Route("mvc/orders")]
public class StreamingOrdersController : ControllerBase
{
    [HttpGet("stream")]
    public IResult Stream() => AvroResults.Stream(StreamingTests.Orders(1_000));
}

public class StreamingTests
{
    private WebApplication _app;
    private HttpClient _client;
    private TaskCompletionSource _firstItemReceived;

    internal static async IAsyncEnumerable<Order> Orders(int count, Task waitBeforeLast = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var i = 1; i <= count; i++)
        {
            if (i == count && waitBeforeLast != null)
            {
                await waitBeforeLast.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }

            yield return new Order { Id = i, Customer = $"customer-{i}", Total = i, CreatedAt = new DateTime(2026, 10, 25, 0, 0, 0, DateTimeKind.Utc) };
            await Task.Yield();
        }
    }

    [SetUp]
    public async Task SetUp()
    {
        _firstItemReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        (_app, _client) = await TestServerFactory.StartKestrel(
            services => services.AddControllers().AddApplicationPart(typeof(StreamingOrdersController).Assembly),
            app =>
            {
                app.MapControllers();
                app.MapGet("/orders/stream", (int count) => AvroResults.Stream(Orders(count)));
                app.MapGet("/orders/stream-until-read", () => AvroResults.Stream(Orders(20_000, _firstItemReceived.Task)));
                app.MapPost("/orders/import", async (AvroStream<Order> orders) =>
                {
                    var count = 0;
                    long idSum = 0;
                    await foreach (var order in orders)
                    {
                        count++;
                        idSum += order.Id;
                    }

                    return AvroResults.Avro(new ImportSummary { Count = count, IdSum = idSum });
                });
            });
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    public class ImportSummary
    {
        public int Count { get; set; }
        public long IdSum { get; set; }
    }

    [Test]
    public async Task Streamed_response_is_read_item_by_item()
    {
        var ids = new List<int>();
        await foreach (var order in _client.GetFromAvroAsAsyncEnumerable<Order>("orders/stream?count=10000"))
        {
            ids.Add(order.Id);
        }

        ids.Should().Equal(Enumerable.Range(1, 10_000));
    }

    [Test]
    public async Task First_items_arrive_before_the_server_has_produced_the_last_one()
    {
        // The server holds its last item until the client has read the first one: buffering anywhere would deadlock.
        var count = 0;
        await foreach (var _ in _client.GetFromAvroAsAsyncEnumerable<Order>("orders/stream-until-read").WithCancellation(new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token))
        {
            _firstItemReceived.TrySetResult();
            count++;
        }

        count.Should().Be(20_000);
    }

    [Test]
    public async Task Streamed_upload_is_read_item_by_item()
    {
        var response = await _client.PostAsAvroStreamAsync("orders/import", Orders(5_000));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromAvroAsync<ImportSummary>()).Should().BeEquivalentTo(new ImportSummary { Count = 5_000, IdSum = 5_000L * 5_001 / 2 });
    }

    [Test]
    public async Task Streamed_upload_with_json_returns_415()
    {
        var response = await _client.PostAsJsonAsync("orders/import", new[] { TestServerFactory.SampleOrder() });

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task Mvc_action_can_return_a_stream()
    {
        var orders = await _client.GetFromAvroAsAsyncEnumerable<Order>("mvc/orders/stream").ToListAsync();

        orders.Should().HaveCount(1_000);
    }
}
