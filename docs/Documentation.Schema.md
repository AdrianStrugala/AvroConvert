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
string json = AvroConvert.GetSchema(avroFile);                // byte[] or Stream
AvroSchema schema = AvroSchema.FromAvro(avroFile);            // parsed, see AvroSchema below
```

### Generating a schema from C# types

```csharp
AvroSchema schema = AvroSchema.Create<Order>();
string json = schema.Json;                                  // same as AvroConvert.GenerateSchema(typeof(Order))
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
| `Dictionary<K, T>` with other keys | `array` of `{Key, Value}` records (a `string?` value stays `"string"`) |
| `Nullable<T>` (`int?`…), `string?` and other nullable references | `["null", T]`, also for items and values: `List<string?>` → `{"type": "array", "items": ["null", "string"]}`, `Dictionary<string, string?>` → `{"type": "map", "values": ["null", "string"]}` |
| `object` | needs `[AvroUnion(typeof(A), typeof(B))]` listing the possible types |
| `decimal`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan` | logical types, see [Logical types](#logical-types) |

A reference type is nullable when declared with `?` in a nullable context or marked with `[NullableSchema]`. Writing
`null` into a member that is not nullable throws (`NullValueHandling = WriteEmpty` writes an empty value, like 3.x).
Multidimensional arrays (`int[,]`) are not supported – use jagged arrays (`int[][]`).

#### Members and attributes

Public properties and fields are serialized, plus non-public ones marked with `[DataMember]`. `[IgnoreDataMember]`
excludes a member; `IncludeOnlyDataContractMembers = true` keeps only `[DataMember]`
members.

| Attribute | Effect |
|---|---|
| `[DataContract(Name, Namespace)]` | record name and namespace |
| `[DataMember(Name)]` | field name; the .NET name is kept in `aliases` |
| `[IgnoreDataMember]` | member not serialized |
| `[Description("…")]` | `doc` of the record, enum or field |
| `[DefaultValue(…)]` | field `default`, used when reading data that lacks the field; a `null` written into the field stays `null` |
| `[NullableSchema]` | field type becomes `["null", T]` |
| `[AvroDecimal(Precision, Scale)]` | precision and scale of a `decimal` |
| `[AvroType(AvroTypeRepresentation…)]` | another Avro representation, e.g. `timestamp-millis` (see [Logical types](#logical-types)) |
| `[AvroUnion(typeof(A), typeof(B))]` | union of the listed types for an `object` member; `null` in the list sets the position of the null branch: `[AvroUnion(typeof(int), null)] int?` → `["int", "null"]` |
| `[AvroFixed(Size, Name, Namespace)]` | `byte[]` as a named `fixed`; on a `decimal`, a `decimal` on that `fixed` |

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

Reading accepts every logical type of the Avro 1.12 specification. An unknown logical type is ignored and the
underlying type is used. `decimal` on `fixed`, as Java and Confluent schemas often have it:

```csharp
[AvroFixed(16, "Money")]
[AvroDecimal(Precision = 20, Scale = 2)]
public decimal Amount { get; set; }
```

A timestamp member declared as `long` holds the raw count since the epoch and is written and read as is. This keeps
`*-nanos` values exact, which `DateTime` (100 ns resolution) cannot:

```csharp
[AvroType(AvroTypeRepresentation.TimestampNanoseconds)]
public long CreatedAtNanos { get; set; }
```

`big-decimal` is currently implemented only by the Java and Rust Avro libraries (and AvroConvert); prefer `decimal`
for data shared with other implementations.

Dates and times in a `string` field (Chr.Avro writes them so by default) are read and written as ISO 8601 text.
`DateTime` also reads `date` and `TimeSpan` reads `time-millis` / `time-micros`, as in Apache.Avro `avrogen` classes.


### Generate model

Generates C# classes from a schema or from an Avro file (the schema in its header) – the reverse of `GenerateSchema`:

```csharp
string code = AvroConvert.GenerateModel(avroFile);     // byte[]
string code = AvroConvert.GenerateModel(avroSchema);   // AvroSchema
```

```json
{"name":"User","namespace":"Shop","type":"record","fields":[
  {"name":"name","type":"string"},
  {"name":"favorite_number","type":["null","int"]},
  {"name":"createdAt","type":{"type":"long","logicalType":"timestamp-millis"}}]}
```
```csharp
using System;
using System.Runtime.Serialization;
using SolTechnology.Avro.Infrastructure.Attributes;

[DataContract(Namespace = "Shop")]
public class User
{
	public string name { get; set; }
	public int? favorite_number { get; set; }
	[AvroType(AvroTypeRepresentation.TimestampMilliseconds)]
	public DateTime createdAt { get; set; }
}
```

| Avro | C# |
|---|---|
| `record`, `enum` | class, enum |
| `["null", T]`, `[T, "null"]` | `T?` |
| other unions | `object` with `[AvroUnion]` listing every branch |
| `array`, `map` | `T[]`, `Dictionary<string, T>` |
| `bytes`, `fixed` | `byte[]`, `byte[]` with `[AvroFixed]` |
| `decimal`, `uuid`, `date` | `decimal`, `Guid`, `DateOnly` |
| `timestamp-*`, `time-*`, `duration` | `DateTime`, `TimeOnly`, `TimeSpan` |

The classes give back the source schema (`AvroSchema.Create<User>()`): attributes keep names, namespaces, logical
types, the position of `null` in unions and `doc` (`[Description]`). The code compiles as it is, also without
`<Nullable>enable`. Exceptions: `error` becomes `record`; in array items and map values `null` goes first and `decimal`
on `fixed` becomes `decimal` on `bytes`. These read and write the same data.

### AvroSchema

`AvroSchema` is a parsed, immutable schema with its identity (canonical form, fingerprints) and the schema evolution
checks. The `AvroConvert` methods that take a schema accept it next to the JSON string, which saves parsing it again.

```csharp
AvroSchema schema = AvroSchema.Parse(json);              // schema definition (JSON)
AvroSchema schema = AvroSchema.Parse(json, new[] { addressJson, statusJson });   // uses types defined in other schemas
AvroSchema schema = AvroSchema.Create<Order>();          // from a .NET type (same as AvroConvert.GenerateSchema)
AvroSchema schema = AvroSchema.FromValue(value);         // from a value, also what it holds as `object`
AvroSchema schema = AvroSchema.FromAvro(avroFile);       // from the header of an Avro file (byte[] or Stream)
AvroSchema schema = AvroSchema.InferFromJson(jsonData);  // inferred from sample JSON data

string json      = schema.Json;                          // also ToString()
string canonical = schema.CanonicalForm;                 // Parsing Canonical Form
long   rabin     = schema.Fingerprint;                   // CRC-64-AVRO
byte[] sha256    = schema.GetFingerprint(AvroFingerprintAlgorithm.Sha256);   // also Md5, Crc64Avro (8 bytes)
```

The canonical form keeps only what defines the binary data (no doc, aliases, defaults or logical types). Schemas with
the same canonical form are equal and read and write identical bytes. Attributes the specification does not define
(e.g. Iceberg `field-id`) stay in `Json` and are left out of the canonical form.

#### Writing with a given schema

By default a container is written with the schema of the serialized type. Every `Serialize` has an overload that takes
the schema instead. It goes to the file header with all its attributes, so the file can carry what other tools expect,
like the `field-id` of Apache Iceberg:

```csharp
var schema = AvroSchema.Parse("""
    {"type":"record","name":"manifest_entry","fields":[
        {"name":"status","type":"int","field-id":0}]}
    """);

byte[] avro = AvroConvert.Serialize(entries, schema);                       // options: Serialize(entries, schema, options)
AvroConvert.Serialize(entries, fileStream, schema);                         // items are streamed, never collected
await AvroConvert.Serialize(asyncEntries, fileStream, schema);
AvroConvert.Serialize(entries, bufferWriter, schema);
```

A collection is written as one entry per item, unless the schema is an array, which holds the whole collection in one
entry. Items do not need a class of their own: a class, an `ExpandoObject` and a `Dictionary<string, object>` are written
as the record the schema describes, a member or a key missing for a field with a default gives the default.

#### Comparing schemas

```csharp
SchemaComparison comparison = AvroSchema.Compare(writer: v1, reader: v2);

comparison.AreEquivalent;   // same canonical form
comparison.CanRead;         // v2 reads data written with v1
foreach (var d in comparison.Differences)
    Console.WriteLine(d);   // e.g. "Order.lines[].price: decimal(10,2) → decimal(10,4) (breaking)"
```

The comparison follows the schema resolution rules of the specification. Each `SchemaDifference` has `Path`, `Kind`,
`IsBreaking` and `Description`.

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
