using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SolTechnology.Avro;
using SolTechnology.Avro.Http;

var builder = WebApplication.CreateBuilder(args);

// MVC controllers: JSON stays the default, Avro is used for Content-Type / Accept: application/avro.
builder.Services.AddControllers().AddAvroFormatters(new AvroConvertOptions { Codec = CodecType.Brotli });

// Minimal API endpoints read their AvroConvert options from here.
builder.Services.Configure<AvroHttpOptions>(o => o.SerializerOptions = new AvroConvertOptions { Codec = CodecType.Brotli });

var app = builder.Build();
app.MapControllers();

// Minimal API: Avro<T> binds an application/avro body, AvroResults.Avro writes one.
app.MapGet("/minimal/forecast", () => AvroResults.Avro(WeatherForecast.Next(5)));
app.MapPost("/minimal/forecast", (Avro<WeatherForecast> forecast) =>
    AvroResults.Avro(forecast.Value, statusCode: StatusCodes.Status201Created));

app.Run("http://localhost:2137");

public record WeatherForecast(DateTime Date, int TemperatureC, string Summary)
{
    private static readonly string[] Summaries = { "Freezing", "Chilly", "Mild", "Warm", "Hot" };

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public static WeatherForecast[] Next(int days) => Enumerable.Range(1, days)
        .Select(day => new WeatherForecast(DateTime.UtcNow.Date.AddDays(day), Random.Shared.Next(-20, 40), Summaries[Random.Shared.Next(Summaries.Length)]))
        .ToArray();
}

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    [HttpGet]
    public WeatherForecast[] Get() => WeatherForecast.Next(5);

    [HttpPost]
    public WeatherForecast Post(WeatherForecast forecast) => forecast;
}
