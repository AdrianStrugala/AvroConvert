### Serialization
```csharp
 byte[] avroObject = AvroConvert.Serialize(object yourObject);
```

Using encoding
```csharp
 byte[] avroObject = AvroConvert.Serialize(object yourObject, CodecType.Snappy);
```
Supported encoding types:
- Null (default)
- Deflate
- Snappy
- Zstandard
- GZip (non-standard, possibly not supported outside AvroConvert)
- Brotli (non-standard, possibly not supported outside AvroConvert)


### Deserialization

```csharp
//Using generic method
CustomClass deserializedObject = AvroConvert.Deserialize<CustomClass>(byte[] avroObject);

//Using dynamic method
CustomClass deserializedObject = AvroConvert.Deserialize(byte[] avroObject, typeof(CustomClass));

//Deserialization to dynamic result
dynamic deserializedObject = AvroConvert.Deserialize<dynamic>(byte[] avroObject);
```

Records read into `dynamic`, `ExpandoObject` or an `object` member become `ExpandoObject`, unless a loaded class matches the record's namespace **and** name. For a union of records in a typed model, list the alternatives explicitly – they are matched by class name:
```csharp
public class Envelope
{
    [AvroUnion(typeof(OrderCreated), typeof(OrderCancelled))]
    public object Payload { get; set; }
}
```

Deserialization when a property value is null, but schema contains information about default value
```csharp
//Model used for serialization
public class DefaultValueClass
{
    [DefaultValue("Let's go")]
    public string justSomeProperty { get; set; }

    [DefaultValue(2137)]
    public long? andLongProperty { get; set; }
}

//Deserializing object with null data
 DefaultValueClass deserializedObject = AvroConvert.Deserialize<DefaultValueClass>(byte[] avroObject);

//Produces following object:
> deserializedObject.justSomeProperty
> "Let's go"

> deserializedObject.andLongProperty
> 2137
```

### Streaming

Large collections can be written and read without holding them in memory. Each item is one container entry and
data blocks are written or read as the sequence is enumerated. The stream is left open.

```csharp
// write
AvroConvert.Serialize(ReadRowsFromDatabase(), fileStream, new AvroConvertOptions { Codec = CodecType.Zstandard });
await AvroConvert.Serialize(rowsAsyncEnumerable, httpResponseStream, cancellationToken: ct);

// read
await foreach (var row in AvroConvert.Deserialize<Row>(httpRequestStream, cancellationToken: ct))
{
    // process row
}

// into a caller's buffer (e.g. a PipeWriter or ArrayBufferWriter<byte>)
AvroConvert.Serialize(order, bufferWriter);
AvroConvert.SerializeHeadless(order, bufferWriter, AvroSchema.Create<Order>());
```

The schema comes from `T`; for `object`, interfaces, abstract classes and `ExpandoObject` the items are collected
first, because the schema depends on them.

### Deserialization of large collection of Avro objects one by one
```csharp
using (var reader = AvroConvert.OpenDeserializer<CustomClass>(new MemoryStream(avroObject)))
{
    while (reader.HasNext())
    {
        var item = reader.ReadNext();
        // process item
    }
}
```


### Headless serialization and deserialization

**Schemas provided for serialization and deserialization have to be *exactly* the same**

```csharp

string schema = "{"type":"record","name":"user.User","fields":[{"name":"name","type":,"string"},{"name":"favorite_number","type":["null","int"]},{"name":"favorite_color","type":["null","string"]}]}";

var serialized = AvroConvert.SerializeHeadless(toSerialize, schema);

var deserialized = AvroConvert.DeserializeHeadless<User>(serialized, schema);

```

Rows written one after another with the item schema (e.g. by the BigQuery Storage API) are read into a collection
until the end of the data:

```csharp
List<User> users = AvroConvert.DeserializeHeadless<List<User>>(rows, userSchema);
```


### Merge

Merges multiple Avro objects of type T into one of type IEnumerable of T

```csharp

byte[] result = AvroConvert.Merge<T>(IEnumerable<byte[]> avroObjects);

example:
//Arrange
var users = _fixture.CreateMany<User>();
var avroObjects = users.Select(AvroConvert.Serialize);


//Act 
var result = AvroConvert.Merge<User>(avroObjects);


//Assert
var deserializedResult = AvroConvert.Deserialize<List<User>>(result);

```


### Avro2Json

Converts Avro serialized object directly to JSON format. Useful for a minimal API approach.
The JSON has the shape of the .NET model (as produced by `JsonConvert.SerializeObject`), e.g. a nullable field is
`"MyField": "x"`. For the JSON format defined by the Avro specification see [Avro JSON Encoding](#avro-json-encoding).

```csharp

string resultJson = AvroConvert.Avro2Json(byte[] avroData);


example:
//Arrange
var user = _fixture.Creat<User>();
var avroData = AvroConvert.Serialize(user);


//Act 
var resultJson = AvroConvert.Avro2Json(avroData);


//Assert
var expectedJson = JsonConvert.SerializeObject(user);
Assert.Equal(expectedJson, resultJson);

```

### Json2Avro

Converts JSON serialized object directly to Avro format.

```csharp

byte[] resultAvro = Json2Avro<T>(string json, CodecType codecType);

example:
//Arrange
var user = _fixture.Creat<User>();
var jsonData = JsonConvert.SerializeObject(user);


//Act 
var resultAvro = AvroConvert.Json2Avro<User>(jsonData);


//Assert
var expectedAvro = AvroConvert.Serialize(user);
Assert.Equal(expectedAvro, resultAvro);

```

### Avro JSON Encoding

`Avro2JsonEncoding` and `JsonEncoding2Avro` read and write JSON as defined in the "JSON Encoding" section of the Avro
specification – the format of avro-tools `tojson`/`fromjson` and of the JSON encoders in Java, Python and Apache.Avro.
It differs from `Avro2Json`/`Json2Avro` (plain JSON shaped like the .NET model):

| | `Avro2Json` | `Avro2JsonEncoding` |
|---|---|---|
| Union `["null","string"]` | `"x"` | `{"string": "x"}` (`null` stays `null`) |
| Union with a record | `{...}` | `{"com.acme.Address": {...}}` (full name of the branch) |
| `bytes`, `fixed` | Base64 | string of code points 0–255 (`"\u0000Aÿ"`) |
| Logical types | .NET value (`"2024-05-06T07:08:09Z"`) | underlying type (`timestamp-millis`: `1714979289000`) |
| Several container entries | JSON array | one JSON value per line |

```csharp
string json = AvroConvert.Avro2JsonEncoding(byte[] avroFile);                 // object container file, schema from the header
string json = AvroConvert.Avro2JsonEncoding(byte[] headlessAvro, string schema);

byte[] avroFile = AvroConvert.JsonEncoding2Avro(string json, string schema);   // one entry per JSON value
byte[] avroFile = AvroConvert.JsonEncoding2Avro(string json, string schema, CodecType.Zstandard);
```

`JsonEncoding2Avro` requires the schema: JSON alone does not tell e.g. `int` from `long`. Record fields missing from the
JSON take the schema default; unknown properties are ignored. A named union branch may also be given by its short name.

### Licence declaration

AvroConvert is free for noncommercial use and for companies under 100 people / 1,000,000 USD revenue; other
organisations need a commercial licence (see [LICENSE.md](../LICENSE.md)). Declare which licence your application
relies on — functionality is never restricted, and the declaration silences the one-time `Trace` warning emitted
when no licence is declared:

```csharp
AvroConvert.License = AvroLicense.NonCommercial;
AvroConvert.License = AvroLicense.SmallBusiness;
AvroConvert.License = AvroLicense.Commercial("AVC1.…");   // licence key received by e-mail after purchase
```

The same can be done without code changes through the `AVROCONVERT_LICENSE` environment variable:
`NonCommercial`, `SmallBusiness` or the key itself.

A commercial key is a signed token (`AVC1.<payload>.<signature>`, ECDSA P-256) that carries the licence number,
licensee, plan and subscription end. `AvroLicense.Commercial` verifies the signature and exposes the values as
`Id`, `Licensee`, `Plan`, `ValidUntil` and `IsSignatureValid`; a malformed string throws `ArgumentException`,
while a key with a wrong signature or a subscription that ended before this version was released is accepted
and only produces a `Trace` warning (versions released during the subscription stay licensed for ever).

### Options reference (4.0)

| Option | Default | Description |
|---|---|---|
| `Codec` | `Null` | Container codec: `Null`, `Deflate`, `Snappy`, `Zstandard`, `GZip`, `Brotli` (the last two are non-standard, possibly not supported outside AvroConvert) |
| `CollectionMode` | `Entries` | Top-level collection written as one container entry per element (like other Avro tools) or as a single `array` (`SingleArray`, 3.x layout) |
| `MissingFieldHandling` | `Throw` | Reader field absent from the writer and without default: throw (spec) or leave the CLR default (`UseDefault`). Fields that can hold `null` always resolve to `null` |
| `NumberHandling` | `Strict` | Decimal scale overflow: `Strict`, `Truncate`, `Rounding` |
| `IncludeOnlyDataContractMembers` | `false` | Serialize only members marked with `[DataMember]` |
| `NamingPolicy` | – | Custom member/enum naming |
| `DateTimeRepresentation` | `null` (`timestamp-micros`) | Logical type of `DateTime`/`DateTimeOffset` members without `[AvroType]`: `TimestampMilliseconds`, `TimestampMicroseconds`, `TimestampNanoseconds` or a `LocalTimestamp*` value |
| `AvroConverters` | – | Custom `IAvroConverter` implementations |

### Trimming and Native AOT

AvroConvert maps .NET types by reflection and compiles serializers at runtime. Methods that do so are marked with
`[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`, so a trimmed or Native AOT application gets a warning where
it calls them instead of failing at run time. This includes `AvroSchema.Parse`, because field `default` values are
materialized by reflection.
