### Reading Avro schema from Avro encoded object
```csharp
string schemaInJsonFormat = AvroConvert.GetSchema(byte[] avroObject)
```


### Generate Model

Generates C# .NET classes (models) from Avro file or schema.
This action is inversed to GenerateSchema.

```csharp
//Given is schema
var schema = AvroConvert.GenerateSchema(typeof(User));

{
   "type":"record",
   "name":"AvroConvertComponentTests.User",
   "fields":[
      {
         "name":"name",
         "type":"string"
      },
      {
         "name":"favorite_number",
         "type":[
            "null",
            "int"
         ]
      },
      {
         "name":"favorite_color",
         "type":"string"
      }
   ]
}

//Action
string resultModel = AvroConvert.GenerateModel(schema); //string

OR

string resultModel = AvroConvert.GenerateModel(avroFileContent); //byte[]

//Produces following model:
public class User
{
    public string name { get; set; }
    public int? favorite_number { get; set; }
    public string? favorite_color { get; set; }
}


```


### Generating Avro schema for C# classes

Using simple class
```csharp

//Model
public class SimpleTestClass
{
    public string justSomeProperty { get; set; }
    public long andLongProperty { get; set; }
}


//Action
string schemaInJsonFormat = AvroConvert.GenerateSchema(typeof(SimpleTestClass));


//Produces following schema:
{
   "type":"record",
   "name":"AvroConvert.SimpleTestClass",
   "fields":[
      {
         "name":"justSomeProperty",
         "type":[
            "null",
            "string"
         ]
      },
      {
         "name":"andLongProperty",
         "type":"long"
      }
   ]
}
```

Using class decorated with attributes
```csharp
//Model
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


//Action
string schemaInJsonFormat = AvroConvert.GenerateSchema(typeof(AttributeClass));


//Produces following schema:
{
  "type": "record",
  "name": "userspace.User",
  "doc": "This is Doc of User Class",
  "fields": [
    {
      "name": "name",
      "doc": "This is Doc of record field",
      "aliases": [
        "StringProperty"
      ],
      "type": "string"
    },
    {
      "name": "favorite_number",
      "aliases": [
        "NullableIntPropertyWithDefaultValue"
      ],
      "type": [
        "int",
        "null"
      ],
      "default": 2137
    },
    {
      "name": "not_favorite_number",
      "aliases": [
        "AnotherWayOrIndicatingNullableProperty"
      ],
      "type": [
        "null",
        "int"
      ]
    },
    {
      "name": "favorite_color",
      "aliases": [
        "AndAnotherString"
      ],
      "type": [
        "null",
        "string"
      ]
    }
  ]
}
```  


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
