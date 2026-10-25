using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AvroConvertHttpTests;

public sealed class Order
{
    public int Id { get; set; }
    public string Customer { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

internal static class TestServerFactory
{
    public static async Task<(WebApplication App, HttpClient Client)> Start(Action<IServiceCollection> services, Action<WebApplication> endpoints)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        services(builder.Services);
        var app = builder.Build();
        endpoints(app);
        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    /// <summary>Runs on Kestrel: status codes of BadHttpRequestException are applied by the server, not by TestServer.</summary>
    public static async Task<(WebApplication App, HttpClient Client)> StartKestrel(Action<IServiceCollection> services, Action<WebApplication> endpoints)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        services(builder.Services);
        var app = builder.Build();
        endpoints(app);
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return (app, new HttpClient { BaseAddress = new Uri(address + "/") });
    }

    public static Order SampleOrder() => new()
    {
        Id = 42,
        Customer = "Sol Technology",
        Total = 199.99m,
        CreatedAt = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc)
    };
}
