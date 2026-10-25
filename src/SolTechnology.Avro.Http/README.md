# SolTechnology.Avro.Http

Apache Avro (`application/avro`) as the wire format between HTTP services, built on [AvroConvert](https://www.nuget.org/packages/AvroConvert). The API mirrors `System.Net.Http.Json`: plain C# classes in and out, no code generation.

```
dotnet add package SolTechnology.Avro.Http
```

## HttpClient

```csharp
var order = await httpClient.GetFromAvroAsync<Order>("orders/1");        // sends Accept: application/avro
using var response = await httpClient.PostAsAvroAsync("orders", order);  // also PutAsAvroAsync, PatchAsAvroAsync
var created = await response.Content.ReadFromAvroAsync<Order>();
var content = AvroContent.Create(order, new AvroConvertOptions { Codec = CodecType.Brotli });
```

## ASP.NET Core

```csharp
// MVC controllers: JSON stays the default, Avro is used for Content-Type / Accept: application/avro
builder.Services.AddControllers().AddAvroFormatters();

// Minimal APIs
app.MapPost("/orders", (Avro<Order> order) => Results.Extensions.Avro(order.Value, statusCode: 201));
app.MapGet("/orders/{id}", (int id) => AvroResults.Avro(repository.Get(id)));
```

A body that is not valid Avro is a 400. A Minimal API `Avro<T>` parameter answers 415 to other content types.

## Streaming

Large results and uploads go over the wire block by block. Only the current block is held in memory, on both sides.

```csharp
app.MapGet("/orders", (OrderStore store) => AvroResults.Stream(store.ReadAllAsync()));   // IAsyncEnumerable<Order>
app.MapPost("/orders/import", async (AvroStream<Order> orders) =>
{
    await foreach (var order in orders) { /* … */ }
});

await foreach (var order in httpClient.GetFromAvroAsAsyncEnumerable<Order>("orders")) { /* … */ }
await httpClient.PostAsAvroStreamAsync("orders/import", ReadOrdersAsync());
```

Every method takes optional `AvroConvertOptions` (codec, naming policy, converters) and a `CancellationToken`.

## Links

- [Documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#http)
- [Source and samples](https://github.com/AdrianStrugala/AvroConvert)
- [AvroConvert Online](https://soltechnology.dev/avroconvert/online/): schema → C#, Avro file viewer and more, in your browser

## Licence

Free for noncommercial use and for companies under 100 people and USD 1M revenue (PolyForm Noncommercial / Small Business). Other organisations need a commercial licence: [soltechnology.dev/avroconvert](https://soltechnology.dev/avroconvert/). See LICENSE.md in the package.
