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
- GZip
- Brotli


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

Converts Avro serialized object directly to JSON format. Useful for a minimal API approach

```csharp

string resultJson = AvroConvert.Json2Avro(byte[] avroData);


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
| `Codec` | `Null` | Container codec: `Null`, `Deflate`, `Snappy`, `GZip`, `Brotli` (the last two are AvroConvert extensions, not portable) |
| `CollectionMode` | `Entries` | Top-level collection written as one container entry per element (like other Avro tools) or as a single `array` (`SingleArray`, 3.x layout) |
| `MissingFieldHandling` | `Throw` | Reader field absent from the writer and without default: throw (spec) or leave the CLR default (`UseDefault`). Fields that can hold `null` always resolve to `null` |
| `NumberHandling` | `Strict` | Decimal scale overflow: `Strict`, `Truncate`, `Rounding` |
| `IncludeOnlyDataContractMembers` | `false` | Serialize only members marked with `[DataMember]` |
| `NamingPolicy` | – | Custom member/enum naming |
| `AvroConverters` | – | Custom `IAvroConverter` implementations |
