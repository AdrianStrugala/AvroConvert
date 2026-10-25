using System;
using System.Net.Http;
using System.Text.Json;
using SolTechnology.Avro.Http;

// Start samples/Http.Server first.
using var client = new HttpClient { BaseAddress = new Uri("http://localhost:2137/") };

// GetFromAvroAsync sends Accept: application/avro, so the server answers in Avro instead of JSON.
var forecasts = await client.GetFromAvroAsync<WeatherForecast[]>("WeatherForecast");
Console.WriteLine($"MVC GET     -> {JsonSerializer.Serialize(forecasts)}");

using var posted = await client.PostAsAvroAsync("WeatherForecast", forecasts[0]);
Console.WriteLine($"MVC POST    -> {(int)posted.StatusCode} {posted.Content.Headers.ContentType}");

var minimal = await client.GetFromAvroAsync<WeatherForecast[]>("minimal/forecast");
Console.WriteLine($"Minimal GET -> {minimal.Length} forecasts");

using var created = await client.PostAsAvroAsync("minimal/forecast", minimal[0]);
var echoed = await created.Content.ReadFromAvroAsync<WeatherForecast>();
Console.WriteLine($"Minimal POST-> {(int)created.StatusCode} {JsonSerializer.Serialize(echoed)}");

public record WeatherForecast(DateTime Date, int TemperatureC, string Summary);
