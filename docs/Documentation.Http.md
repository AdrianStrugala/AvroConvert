# SolTechnology.Avro.Http

`dotnet add package SolTechnology.Avro.Http --prerelease`

Serialization itself is described in [Documentation.md](Documentation.md); options are in the
[Options reference](Documentation.md#options-reference).

`SolTechnology.Avro.Http` uses Avro (`application/avro`) as the wire format between HTTP services, mirroring
`System.Net.Http.Json`:

```csharp
// Client
var order = await httpClient.GetFromAvroAsync<Order>("orders/1");        // sends Accept: application/avro
using var response = await httpClient.PostAsAvroAsync("orders", order);  // also PutAsAvroAsync, PatchAsAvroAsync
var created = await response.Content.ReadFromAvroAsync<Order>();
var content = AvroContent.Create(order, new AvroConvertOptions { Codec = CodecType.Brotli });

// MVC controllers: JSON stays the default, Avro is used for Content-Type / Accept: application/avro
builder.Services.AddControllers().AddAvroFormatters(new AvroConvertOptions { Codec = CodecType.Brotli });

// Minimal API
app.MapPost("/orders", (Avro<Order> order) => Results.Extensions.Avro(order.Value, statusCode: 201));
app.MapGet("/orders/{id}", (int id) => AvroResults.Avro(repository.Get(id)));
builder.Services.Configure<AvroHttpOptions>(o => o.SerializerOptions = new AvroConvertOptions { … });
```

Every method takes optional `AvroConvertOptions` and a `CancellationToken`. A request body that is not valid Avro is a
400 (a model state error in MVC); a Minimal API `Avro<T>` parameter answers 415 to other content types.

Large results and uploads can be streamed as an Avro object container: each data block goes over the wire as soon as
it is full and only the current block is held in memory, on both sides.

```csharp
// Server (Minimal API, or returned from an MVC action)
app.MapGet("/orders", (OrderStore store) => AvroResults.Stream(store.ReadAllAsync()));     // IAsyncEnumerable<Order>
app.MapPost("/orders/import", async (AvroStream<Order> orders) =>
{
    await foreach (var order in orders) { /* … */ }
});

// Client
await foreach (var order in httpClient.GetFromAvroAsAsyncEnumerable<Order>("orders")) { /* … */ }
await httpClient.PostAsAvroStreamAsync("orders/import", ReadOrdersAsync());               // AvroStreamContent<T>
```

Runnable examples are in `samples/Http.Server` (Minimal API and MVC) and `samples/Http.Client`.
