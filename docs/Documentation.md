# AvroConvert documentation

AvroConvert 4.0 targets .NET 10 (`net10.0`); for .NET Framework and .NET 6–9 use 3.4.x. All operations are static
methods of `AvroConvert` in the `SolTechnology.Avro` namespace. Schema-related features (type mapping, attributes,
logical types, `AvroSchema`, model generation) are described in [Documentation.Schema.md](Documentation.Schema.md).

### At a glance

| | Task | Code |
|---|---|---|
| **Write** | [Objects to an Avro file, with a codec](#serialization) | `AvroConvert.Serialize(orders, new AvroConvertOptions { Codec = CodecType.Zstandard })` |
| | [One message without header (Kafka, queues)](#headless-serialization-and-deserialization) | `AvroConvert.SerializeHeadless(order, schema)` |
| | [Stream a large collection](#streaming) | `AvroConvert.Serialize(orders, fileStream)` |
| | [Write into a buffer](#streaming) | `AvroConvert.Serialize(order, bufferWriter)` |
| | [With a given schema](Documentation.Schema.md#writing-with-a-given-schema) | `AvroConvert.Serialize(orders, schema)` |
| | [Own keys in the file header](#header-metadata) | `AvroConvert.Serialize(entries, schema, metadata: keys)` |
| **Read** | [An Avro file into objects](#deserialization) | `AvroConvert.Deserialize<List<Order>>(avro)` |
| | [One message without header](#headless-serialization-and-deserialization) | `AvroConvert.DeserializeHeadless<Order>(bytes, schema)` |
| | [Old data into a new class](#schema-evolution) | `AvroConvert.Deserialize<OrderV2>(avro)` |
| | [Without a class](#deserialization) | `AvroConvert.Deserialize<List<dynamic>>(avro)` |
| | [Into a dictionary](#deserialization) | `AvroConvert.Deserialize<Dictionary<string, object>>(avro)` |
| | [Keys of the file header](#header-metadata) | `AvroConvert.GetMetadata(avro)` |
| | [Item by item, async](#streaming) | `await foreach (var o in AvroConvert.Deserialize<Order>(stream))` |
| | [Item by item, sync](#reading-items-one-by-one) | `AvroConvert.OpenDeserializer<Order>(stream).ReadNext()` |
| **Schema** | [Of a .NET type](Documentation.Schema.md#generating-a-schema-from-c-types) | `AvroSchema.Create<Order>()` |
| | [Of Avro data](Documentation.Schema.md#reading-the-schema-of-avro-data) | `AvroConvert.GetSchema(avro)` |
| | [Of JSON data](Documentation.Schema.md#avroschema) | `AvroSchema.InferFromJson(json)` |
| | [C# classes from a schema](Documentation.Schema.md#generate-model) | `AvroConvert.GenerateModel(schema)` |
| | [Logical types (dates, decimals, UUID)](Documentation.Schema.md#logical-types) | `[AvroType(AvroTypeRepresentation.TimestampNanoseconds)]` |
| | [Is v2 compatible with v1?](Documentation.Schema.md#avroschema) | `AvroSchema.CheckCompatibility(v1, v2, AvroCompatibilityMode.Backward)` |
| | [Fingerprint, canonical form](Documentation.Schema.md#avroschema) | `schema.Fingerprint` · `schema.CanonicalForm` |
| **Convert** | [JSON → Avro](#json2avro) | `AvroConvert.Json2Avro<Order>(json)` |
| | [Avro → JSON](#avro2json) | `AvroConvert.Avro2Json(avro)` |
| | [Avro JSON Encoding (spec, avro-tools)](#avro-json-encoding) | `AvroConvert.Avro2JsonEncoding(avro)` |
| | [Merge files](#merge) | `AvroConvert.Merge<Order>(files)` |
| **Customize** | [Options](#options-reference) | `new AvroConvertOptions { Codec = …, NullValueHandling = … }` |
| | [Naming policy](#naming-policy) | `new AvroConvertOptions { NamingPolicy = new JavaAvroNamingPolicy("com.acme") }` |
| | [Own conversion of a type](#custom-converters) | `options.AvroConverters.Add(new MoneyConverter())` |
| **Integrate** | [Kafka + Schema Registry](Documentation.Kafka.md) | `producerBuilder.SetAvroValueSerializer(schemaRegistry)` |
| | [HTTP client](Documentation.Http.md) | `await http.GetFromAvroAsync<Order>(url)` |
| | [ASP.NET Core](Documentation.Http.md) | `services.AddControllers().AddAvroFormatters()` |
| | [Command line, schema check in CI](Documentation.Cli.md) | `avroconvert compat v1.avsc v2.avsc` |
| **Run** | [Licence declaration](#licence-declaration) | `AvroConvert.License = AvroLicense.SmallBusiness` |
| | [Trimming and Native AOT](#trimming-and-native-aot) | `[RequiresUnreferencedCode]` warnings show where reflection is used |

### Serialization

`Serialize` writes an Avro object container file: a header with the schema (built from the object's type) followed by
the data.

```csharp
byte[] avro = AvroConvert.Serialize(order);
byte[] compressed = AvroConvert.Serialize(order, new AvroConvertOptions { Codec = CodecType.Zstandard });
```

Codecs:
- `Null` (default)
- `Deflate`
- `Snappy`
- `Zstandard`
- `GZip` (non-standard, possibly not supported outside AvroConvert)
- `Brotli` (non-standard, possibly not supported outside AvroConvert)

A top-level collection with a concrete element type (`List<T>`, `T[]`, `HashSet<T>`, `IEnumerable<T>`…) is written as
one container entry per element, with the element schema – the layout other Avro implementations (Java, Python, Spark,
avro-tools) write and read. `AvroConvertOptions.CollectionMode = AvroCollectionMode.SingleArray` writes the whole
collection as one `array` entry instead (the AvroConvert 3.x layout). Dictionaries are written as one `map` value.

### Deserialization

```csharp
Order order        = AvroConvert.Deserialize<Order>(avro);
List<Order> orders = AvroConvert.Deserialize<List<Order>>(avro);           // every entry of the file
dynamic anything   = AvroConvert.Deserialize<dynamic>(avro);               // without a class
object byType      = AvroConvert.Deserialize(avro, typeof(Order));         // type known at run time
```

Pass the options used for writing (converters, naming policy) as the second argument. A header-only file gives an
empty collection.

A record can also be read into `Dictionary<string, object>` (as a whole or as a member). Keys are the field names,
nested records are dictionaries too. It fits records whose fields change from file to file:

```csharp
public class ManifestEntry
{
    public int Status { get; set; }
    public Dictionary<string, object> Partition { get; set; }
}
```

| Avro | Value, the same as in `dynamic` |
|---|---|
| `int`, `long`, `float`, `double`, `boolean`, `string`, `bytes` | `int`, `long`, `float`, `double`, `bool`, `string`, `byte[]` |
| `decimal` | `decimal` |
| `date` | `DateOnly` |
| `timestamp-*`, `local-timestamp-*` | `DateTime` |
| `time-millis`, `time-micros` | `TimeSpan` |
| `uuid` | `Guid` |
| `enum` | `string` |
| `array` | `List<object>` |
| `map`, `record` | `Dictionary<string, object>` |

Writing is different: a dictionary is an Avro map. To write one as a record,
[give the record schema](Documentation.Schema.md#writing-with-a-given-schema). Values of type `object` are described
[below](#values-of-type-object).

### Values of type object

The type `object` says nothing about the data, so a `Dictionary<string, object>`, an `object` member, a `List<object>`
or an `ExpandoObject` member takes its schema from the values being written:

```csharp
AvroConvert.Serialize(new Dictionary<string, object> { ["a"] = "x" });                  // {"type":"map","values":"string"}
AvroConvert.Serialize(new Dictionary<string, object> { ["a"] = "x", ["b"] = 1L });      // values: ["string","long"]
AvroConvert.Serialize(new Dictionary<string, object> { ["a"] = "x", ["b"] = null });    // values: ["null","string"]
```

- An `ExpandoObject` is a record; a `JObject` is a map.
- The schema depends on the data, so two files of one type can have different schemas.
- Headless data carries no schema: take it from the value and give it to both sides:

```csharp
AvroSchema schema = AvroSchema.FromValue(value);

byte[] data = AvroConvert.SerializeHeadless(value, schema);
var dictionary = AvroConvert.DeserializeHeadless<Dictionary<string, object>>(data, schema);
```

Records read into `dynamic` or `object` become `ExpandoObject`. For a union of records in a typed model, list the
classes – they are matched by name:

```csharp
public class Envelope
{
    [AvroUnion(typeof(OrderCreated), typeof(OrderCancelled))]
    public object Payload { get; set; }
}
```

### Schema evolution

The file carries the schema it was written with (the writer schema); the target type gives the reader schema. They
are resolved as the Avro specification defines:

- fields are matched by name or alias (`[DataMember(Name = …)]` keeps the old name as an alias);
- records are matched by their fields, the record name does not have to be the same (`AvroSchema.CheckCompatibility`
  and Schema Registry are stricter: names have to match);
- writer fields unknown to the reader are skipped;
- a reader field missing in the writer gets its `[DefaultValue]`, or `null` when nullable; otherwise reading throws
  (`MissingFieldHandling = UseDefault` leaves the CLR default, like 3.x);
- `int` → `long` → `float` → `double` and `string` ↔ `bytes` are promoted;
- an enum symbol unknown to the reader resolves to the `default` of the reader's enum schema, when it has one.

```csharp
// written with an older model: public class User { public string Name { get; set; } }
public class User
{
    public string Name { get; set; }

    [DefaultValue(2137)]
    public long Points { get; set; }      // 2137 when reading old data

    public string? Nickname { get; set; } // null when reading old data
}
```

`AvroSchema.Compare` and `AvroSchema.CheckCompatibility` check two schema versions before any data is read – see
[AvroSchema](Documentation.Schema.md#avroschema).

### Streaming

Large collections can be written and read without holding them in memory. Each item is one container entry and data
blocks are written or read as the sequence is enumerated. The stream is left open.

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

The schema comes from `T`. When `T` holds `object` values, the items are collected first to build it; pass a schema
(`Serialize(items, stream, schema)`) to stream them right away.

### Reading items one by one

Synchronous alternative to `Deserialize<T>(Stream)` for a seekable stream (file, `MemoryStream`):

```csharp
using var reader = AvroConvert.OpenDeserializer<Order>(File.OpenRead("orders.avro"));
while (reader.HasNext())
{
    Order order = reader.ReadNext();
    // process order
}
```

### Headless serialization and deserialization

Headless data is the binary encoding of a single value without the container header – the schema has to be known to
both sides (e.g. from a schema registry or a contract).

```csharp
AvroSchema schema = AvroSchema.Parse("""
{"type":"record","name":"User","namespace":"user","fields":[
  {"name":"name","type":"string"},
  {"name":"favorite_number","type":["null","int"]},
  {"name":"favorite_color","type":["null","string"]}]}
""");

byte[] data = AvroConvert.SerializeHeadless(user, schema);
User back   = AvroConvert.DeserializeHeadless<User>(data, schema);
```

Without a schema at hand, `SerializeHeadless(user, typeof(User))` and `DeserializeHeadless<User>(data)` build it from
the type. Data written with another version of the schema needs both (see [Schema evolution](#schema-evolution)):

```csharp
UserV2 user = AvroConvert.DeserializeHeadless<UserV2>(data, writerSchema: v1, readerSchema: v2);
```

Rows written one after another (e.g. by the BigQuery Storage API) are read into a collection; `numberOfRows` limits
how many:

```csharp
List<User> users = AvroConvert.DeserializeHeadless<List<User>>(rows, schema);
```

### Header metadata

The header of an Avro file holds a key-value map. Besides `avro.schema` and `avro.codec` it can hold your own keys, as
some formats require:

```csharp
var keys = new Dictionary<string, AvroMetadataValue>
{
    ["format-version"] = "2",                // text goes in as UTF-8
    ["checksum"] = new byte[] { 1, 2, 3 }    // values are bytes in the Avro specification
};

byte[] avro = AvroConvert.Serialize(entries, AvroSchema.Create<Entry>(), metadata: keys);

IReadOnlyDictionary<string, byte[]> header = AvroConvert.GetMetadata(avro);   // byte[] or Stream
string version = Encoding.UTF8.GetString(header["format-version"]);
```

Every `Serialize` overload that takes a schema takes `metadata` too. Keys starting with `avro.` are reserved by the
specification and throw.

### Merge

Merges Avro files with entries of type `T` into one file with all the entries:

```csharp
IEnumerable<byte[]> files = users.Select(u => AvroConvert.Serialize(u));

byte[] merged = AvroConvert.Merge<User>(files);

List<User> all = AvroConvert.Deserialize<List<User>>(merged);
```

A [header key](#header-metadata) is kept when all the files have the same value for it, and dropped otherwise.

### Avro2Json

Converts Avro data to plain JSON, shaped like the .NET model; several entries become a JSON array. For the JSON format
of the Avro specification see [Avro JSON Encoding](#avro-json-encoding).

```csharp
string json = AvroConvert.Avro2Json(avroFile);
// {"name":"a","favorite_number":1,"favorite_color":"red"}

string fromHeadless = AvroConvert.Avro2Json(headlessData, schema);   // headless data needs its schema
string asV2         = AvroConvert.Avro2Json(avroFile, v2);           // a file is resolved to a reader schema
```

### Json2Avro

Converts JSON directly to an Avro file:

```csharp
byte[] avro = AvroConvert.Json2Avro<User>(json);                    // schema from User
byte[] avro = AvroConvert.Json2Avro<User>(json, new AvroConvertOptions { Codec = CodecType.Deflate });
byte[] avro = AvroConvert.Json2Avro(json);                          // schema inferred from the JSON
```

Without a type the schema is inferred from the JSON (like `AvroSchema.InferFromJson`): integers become `long`,
fractions `double`, nested objects records. Prefer the generic overload when a model exists.

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
string json = AvroConvert.Avro2JsonEncoding(avroFile);                 // schema from the header
string fromHeadless = AvroConvert.Avro2JsonEncoding(headlessData, schema);

byte[] avro = AvroConvert.JsonEncoding2Avro(json, schema);             // one entry per JSON value
```

`JsonEncoding2Avro` needs the schema: JSON alone does not tell `int` from `long`.

### Custom converters

A converter takes over the serialization of a .NET type, e.g. to store it as a `string`:

```csharp
public sealed class MoneySchema : BaseConverterSchema
{
    public MoneySchema() : base(typeof(Money), AvroType.String, "string") { }
}

public sealed class MoneyConverter : IAvroConverter
{
    public TypeSchema TypeSchema { get; } = new MoneySchema();

    public void Serialize(object data, IWriter writer) =>
        writer.WriteString(((Money)data).ToString(CultureInfo.InvariantCulture));

    public object Deserialize(IReader reader) => Money.Parse(reader.ReadString(), CultureInfo.InvariantCulture);
}

var options = new AvroConvertOptions { AvroConverters = { new MoneyConverter() } };
byte[] avro = AvroConvert.Serialize(order, options);              // "Total" field: "string"
Order order = AvroConvert.Deserialize<Order>(avro, options);
```

Pass the same options when reading. Converters are matched by the runtime type of `TypeSchema`.

### Naming policy

`AvroConvertOptions.NamingPolicy` maps .NET names to Avro names. `JavaAvroNamingPolicy` gives camelCase field names
(the .NET name is kept as an alias, so data written before still reads) and one namespace for all types:

```csharp
var options = new AvroConvertOptions { NamingPolicy = new JavaAvroNamingPolicy("com.acme") };
AvroConvert.GenerateSchema(typeof(Order), options);
// {"name":"Order","namespace":"com.acme","type":"record","fields":[{"name":"id","aliases":["Id"],"type":"int"},…]}
```

Implement `IAvroNamingPolicy` (`GetTypeName`, `GetMemberName`) for other conventions.

### HTTP, Kafka and the command line

The satellite packages have their own pages:

- [SolTechnology.Avro.Http](Documentation.Http.md): HttpClient extensions, MVC formatters, Minimal API, streaming
- [SolTechnology.Avro.Kafka](Documentation.Kafka.md): Confluent Schema Registry serializers, plain Avro serializers
- [SolTechnology.Avro.Cli](Documentation.Cli.md): the `avroconvert` tool with avro-tools commands (`induce`, `compile`, `tojson`, `getschema`, `canonical`, `fingerprint`), compatibility and Schema Registry checks for CI

### Licence declaration

AvroConvert is free for noncommercial use and for companies under 100 people / 1,000,000 USD revenue; other
organisations need a commercial licence (see [LICENSE.md](../LICENSE.md)). Declaring the licence silences the one-time
notice on standard error; functionality is never restricted:

```csharp
AvroConvert.License = AvroLicense.NonCommercial;
AvroConvert.License = AvroLicense.SmallBusiness;
AvroConvert.License = AvroLicense.Commercial("AVC1.…");   // licence key received by e-mail after purchase
```

The same can be done without code changes through the `AVROCONVERT_LICENSE` environment variable:
`NonCommercial`, `SmallBusiness` or the key itself.

An invalid or expired key only brings the notice back; nothing stops working. Versions released during the
subscription stay licensed for ever.

### Options reference

| Option | Default | Description |
|---|---|---|
| `Codec` | `Null` | Container codec: `Null`, `Deflate`, `Snappy`, `Zstandard`, `GZip`, `Brotli` (the last two are non-standard, possibly not supported outside AvroConvert) |
| `CollectionMode` | `Entries` | Top-level collection written as one container entry per element (like other Avro tools) or as a single `array` (`SingleArray`, 3.x layout) |
| `MissingFieldHandling` | `Throw` | Reader field absent from the writer and without default: throw (spec) or leave the CLR default (`UseDefault`). Fields that can hold `null` always resolve to `null` |
| `NullValueHandling` | `Throw` | `null` written into a schema that cannot hold `null` (no `["null", T]` union): throw with the field name (spec) or write the empty value – `""`, empty bytes/array/map, `0`, `false` (`WriteEmpty`, 3.x behaviour; enums and logical types still throw) |
| `NumberHandling` | `Strict` | `decimal` with more fraction digits than the schema scale: throw (`Strict`), `Truncate` or `Rounding` |
| `IncludeOnlyDataContractMembers` | `false` | Serialize only members marked with `[DataMember]` |
| `NamingPolicy` | – | Avro names for types and members, see [Naming policy](#naming-policy) |
| `DateTimeRepresentation` | `null` (`timestamp-micros`) | Logical type of `DateTime`/`DateTimeOffset` members without `[AvroType]`: `TimestampMilliseconds`, `TimestampMicroseconds`, `TimestampNanoseconds` or a `LocalTimestamp*` value |
| `AvroConverters` | – | Custom `IAvroConverter` implementations, see [Custom converters](#custom-converters) |

### Trimming and Native AOT

AvroConvert maps .NET types by reflection and compiles serializers at runtime. Methods that do so are marked with
`[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`, so a trimmed or Native AOT application gets a warning where
it calls them instead of failing at run time. This includes `AvroSchema.Parse`, because field `default` values are
materialized by reflection.
