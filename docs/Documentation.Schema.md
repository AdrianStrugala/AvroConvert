# Schemas

- [Reading the schema of Avro data](#reading-the-schema-of-avro-data)
- [Generating a schema from C# types](#generating-a-schema-from-c-types)
  - [Type mapping](#type-mapping)
  - [Members and attributes](#members-and-attributes)
- [Logical types](#logical-types)
- [Generate model](#generate-model)
- [AvroSchema](#avroschema)

### Reading the schema of Avro data

```csharp
string schema = AvroConvert.GetSchema(avroFile);              // byte[]
string schema = AvroConvert.GetSchema("orders.avro");         // file path
string schema = AvroConvert.GetSchema(stream);                // Stream

AvroSchema schema = AvroSchema.FromAvro(avroFile);            // parsed, see AvroSchema below
```

### Generating a schema from C# types

```csharp
string schema = AvroConvert.GenerateSchema(typeof(Order));
string schema = AvroConvert.GenerateSchema(typeof(Order), new AvroConvertOptions { NamingPolicy = … });
AvroSchema schema = AvroSchema.Create<Order>();
```

`Serialize` uses the same schema, so the options that affect it (`IncludeOnlyDataContractMembers`, `NamingPolicy`,
`DateTimeRepresentation`, `AvroConverters`) have to be the same when generating the schema.

```csharp
namespace Shop;

public class SimpleTestClass
{
    public string justSomeProperty { get; set; }
    public long andLongProperty { get; set; }
}

AvroConvert.GenerateSchema(typeof(SimpleTestClass));
```
```json
{
  "name": "SimpleTestClass",
  "namespace": "Shop",
  "type": "record",
  "fields": [
    { "name": "justSomeProperty", "type": "string" },
    { "name": "andLongProperty", "type": "long" }
  ]
}
```

#### Type mapping

| .NET type | Avro schema |
|---|---|
| `bool` | `boolean` |
| `int`, `uint`, `short`, `ushort`, `byte`, `sbyte`, `char` | `int` |
| `long`, `ulong` | `long` |
| `float`, `double` | `float`, `double` |
| `string`, `Uri` | `string` |
| `byte[]` | `bytes` |
| `enum` | `enum` with the member names as symbols |
| class, struct, record | `record` named after the type, the .NET namespace as namespace; closed generic types get the type arguments in the name, e.g. `Field_String` |
| `T[]`, `List<T>`, `HashSet<T>`, `IEnumerable<T>`… | `array` |
| `Dictionary<string, T>`, `Dictionary<Uri, T>` | `map` |
| `Dictionary<K, T>` with other keys | `array` of `{Key, Value}` records |
| `Nullable<T>` (`int?`…), `string?` and other nullable references | `["null", T]` |
| `object` | needs `[AvroUnion(typeof(A), typeof(B))]` listing the possible types |
| `decimal`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan` | logical types, see [Logical types](#logical-types) |

A reference type is nullable when it is declared with `?` in a nullable context or marked with `[NullableSchema]`;
otherwise its schema has no `null` branch, and serializing `null` into it throws, as the Avro specification has no
representation for it (`AvroConvertOptions.NullValueHandling = AvroNullValueHandling.WriteEmpty` writes `""`, an empty
collection or `0` instead, like 3.x). Multidimensional arrays (`int[,]`) are not supported – use jagged arrays (`int[][]`).

#### Members and attributes

Public properties and fields are serialized, plus non-public ones marked with `[DataMember]`. `[IgnoreDataMember]`
excludes a member; `IncludeOnlyDataContractMembers = true` (or `GenerateSchema(type, true)`) keeps only `[DataMember]`
members.

| Attribute | Effect |
|---|---|
| `[DataContract(Name, Namespace)]` | record name and namespace |
| `[DataMember(Name)]` | field name; the .NET name is kept in `aliases` |
| `[IgnoreDataMember]` | member not serialized |
| `[Description("…")]` | `doc` of the record or field |
| `[DefaultValue(…)]` | field `default`, used when reading data that lacks the field |
| `[NullableSchema]` | field type becomes `["null", T]` |
| `[AvroDecimal(Precision, Scale)]` | precision and scale of a `decimal` |
| `[AvroType(AvroTypeRepresentation…)]` | another Avro representation, e.g. `timestamp-millis` (see [Logical types](#logical-types)) |
| `[AvroUnion(typeof(A), typeof(B))]` | union of the listed types for an `object` member |

```csharp
[DataContract(Name = "User", Namespace = "userspace")]
[Description("This is Doc of User Class")]
public class AttributeClass
{
    [DataMember(Name = "name")]
    [Description("This is Doc of record field")]
    public string StringProperty { get; set; }

    [DataMember(Name = "favorite_number")]
    [DefaultValue(2137)]
    public int? NullableIntPropertyWithDefaultValue { get; set; }

    [DataMember(Name = "not_favorite_number")]
    [NullableSchema]
    public int AnotherWayOrIndicatingNullableProperty { get; set; }

    [DataMember(Name = "favorite_color")]
    [NullableSchema]
    public string AndAnotherString { get; set; }
}
```
```json
{
  "name": "User",
  "namespace": "userspace",
  "doc": "This is Doc of User Class",
  "type": "record",
  "fields": [
    { "name": "name", "doc": "This is Doc of record field", "aliases": ["StringProperty"], "type": "string" },
    { "name": "favorite_number", "aliases": ["NullableIntPropertyWithDefaultValue"], "type": ["int", "null"], "default": 2137 },
    { "name": "not_favorite_number", "aliases": ["AnotherWayOrIndicatingNullableProperty"], "type": ["null", "int"] },
    { "name": "favorite_color", "aliases": ["AndAnotherString"], "type": ["null", "string"] }
  ]
}
```

A field with a default puts the default's type first in the union, as the specification requires (`["int", "null"]`).

### Logical types

.NET types map to Avro logical types by default:

| .NET type | Avro schema |
|---|---|
| `DateTime`, `DateTimeOffset` | `long` + `timestamp-micros` |
| `DateOnly` | `int` + `date` |
| `TimeOnly` | `long` + `time-micros` |
| `TimeSpan` | `fixed(12)` + `duration` |
| `Guid` | `string` + `uuid` |
| `decimal` | `bytes` + `decimal` (precision 29, scale 14; change with `[AvroDecimal]`) |

`AvroConvertOptions.DateTimeRepresentation` changes the logical type of all `DateTime`/`DateTimeOffset` members, e.g.
`TimestampMilliseconds` for `timestamp-millis` everywhere. `[AvroType]` picks another representation for a single
member and wins over the option:

```csharp
public class Event
{
    [AvroType(AvroTypeRepresentation.TimestampMilliseconds)]       // timestamp-millis (also TimestampNanoseconds)
    public DateTime CreatedAt { get; set; }

    [AvroType(AvroTypeRepresentation.LocalTimestampMicroseconds)]  // local-timestamp-millis / -micros / -nanos
    public DateTime LocalTime { get; set; }                        // read back with DateTimeKind.Unspecified

    [AvroType(AvroTypeRepresentation.UuidFixed)]                   // uuid on fixed(16), RFC 4122 byte order
    public Guid Id { get; set; }

    [AvroType(AvroTypeRepresentation.BigDecimal)]                  // big-decimal: the scale is stored with every value
    public decimal Amount { get; set; }
}
```

Reading accepts every logical type of the Avro 1.12 specification, including `uuid` on `fixed(16)`, `decimal` on
`fixed` and `big-decimal`. A logical type that is unknown or that annotates an unsupported underlying type (e.g.
`timestamp-millis` on `string`) is ignored and the underlying type is used, as the specification requires.

A timestamp member declared as `long` holds the raw count since the epoch and is written and read as is. This keeps
`*-nanos` values exact, which `DateTime` (100 ns resolution) cannot:

```csharp
[AvroType(AvroTypeRepresentation.TimestampNanoseconds)]
public long CreatedAtNanos { get; set; }
```

`big-decimal` is currently implemented only by the Java and Rust Avro libraries (and AvroConvert); prefer `decimal`
for data shared with other implementations.


### Generate model

Generates C# classes from a schema or from an Avro file (the schema in its header) – the reverse of `GenerateSchema`:

```csharp
string code = AvroConvert.GenerateModel(schemaJson);   // string
string code = AvroConvert.GenerateModel(avroFile);     // byte[]
string code = AvroConvert.GenerateModel(avroSchema);   // AvroSchema
```

```json
{"name":"User","namespace":"Shop","type":"record","fields":[
  {"name":"name","type":"string"},
  {"name":"favorite_number","type":["null","int"]},
  {"name":"favorite_color","type":"string"}]}
```
```csharp
public class User
{
	public string name { get; set; }
	public int? favorite_number { get; set; }
	public string favorite_color { get; set; }
}
```

Records become classes and enums become enums (nested types are generated as separate classes); `["null", T]` becomes
`T?`; arrays become `T[]`, maps `Dictionary<string, T>`; `bytes` and `fixed` become `byte[]`. Logical types: `decimal`
and `big-decimal` → `decimal`, `uuid` → `Guid`, `timestamp-*`, `local-timestamp-*` and `date` → `DateTime`,
`time-*` and `duration` → `TimeSpan`. A union of several records becomes `object` with
`[AvroUnion(typeof(A), typeof(B))]`, so the generated model reads the data back into the right classes.

### AvroSchema

`AvroSchema` is a parsed, immutable schema with its identity (canonical form, fingerprints) and the schema evolution
checks. The `AvroConvert` methods that take a schema accept it next to the JSON string, which saves parsing it again.

```csharp
AvroSchema schema = AvroSchema.Parse(json);              // schema definition (JSON)
AvroSchema schema = AvroSchema.Create<Order>();          // from a .NET type (same as AvroConvert.GenerateSchema)
AvroSchema schema = AvroSchema.FromAvro(avroFile);       // from the header of an Avro file (byte[] or Stream)
AvroSchema schema = AvroSchema.InferFromJson(jsonData);  // inferred from sample JSON data

string json      = schema.Json;                          // also ToString()
string canonical = schema.CanonicalForm;                 // Parsing Canonical Form
long   rabin     = schema.Fingerprint;                   // CRC-64-AVRO
byte[] sha256    = schema.GetFingerprint(AvroFingerprintAlgorithm.Sha256);   // also Md5, Crc64Avro (8 bytes)
```

The Parsing Canonical Form keeps only what defines the binary data: full names, types, fields, symbols, sizes – no doc,
aliases, defaults or logical types, fixed attribute order, no whitespace. Two schemas with the same canonical form are
equal (`Equals`, `GetHashCode`) and read and write identical bytes; the fingerprint identifies a schema version, e.g. as a
key in a schema store.

#### Comparing schemas

```csharp
SchemaComparison comparison = AvroSchema.Compare(writer: v1, reader: v2);

comparison.AreEquivalent;   // same canonical form
comparison.CanRead;         // v2 reads data written with v1
foreach (var d in comparison.Differences)
    Console.WriteLine(d);   // e.g. "Order.lines[].price: decimal(10,2) → decimal(10,4) (breaking)"
```

The comparison follows the schema resolution rules of the specification: records and enums match by name or alias,
fields by name or alias; a field missing in the writer needs a default; an enum symbol unknown to the reader needs the
reader's enum default; unions are matched branch by branch; `int`/`long`/`float`/`double` promote to wider types and
`string`/`bytes` to each other. Each `SchemaDifference` carries its `Path`, `Kind`, `IsBreaking` and `Description`.

#### Compatibility checks

```csharp
SchemaCompatibility result = AvroSchema.CheckCompatibility(previous: v1, next: v2, AvroCompatibilityMode.Backward);
SchemaCompatibility result = AvroSchema.CheckCompatibility(history: new[] { v1, v2 }, next: v3, AvroCompatibilityMode.FullTransitive);

if (!result.IsCompatible)
    throw new InvalidOperationException(string.Join("\n", result.Incompatibilities));
```

The modes mean the same as in Confluent Schema Registry: `Backward` – the new schema reads data written with the previous
one; `Forward` – the previous schema reads data written with the new one; `Full` – both. `*Transitive` modes check every
version in the history instead of only the latest one.
