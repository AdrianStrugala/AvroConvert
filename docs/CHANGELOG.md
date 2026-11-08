**v. 4.0.0 (preview)**

Breaking changes:
- Requires .NET 10. For .NET Framework and .NET 6–9 stay on 3.4.x (`release/3.x`, security and critical fixes)
- New licence: PolyForm Noncommercial / PolyForm Small Business, commercial licence for larger companies. Details: https://soltechnology.dev/avroconvert
- Collections are written as one container entry per item, like in other Avro tools (#118). 3.x files still read. Old layout: `CollectionMode = SingleArray`
- A file with a header but no data deserializes to an empty collection instead of `null`
- A missing non-nullable field throws instead of returning `default` (#87). Old behaviour: `MissingFieldHandling = UseDefault`
- Serializing `null` into a non-nullable field throws instead of writing an empty value (#87). Old behaviour: `NullValueHandling = WriteEmpty`
- A `null` in the data is read as `null`, also when the field has a default (`[DefaultValue]`): the default is only for data that lacks the field, as in the specification and other Avro libraries. 3.x and the previews read the default instead (`Deserialize`, `Avro2Json`, dictionaries)
- Items and values annotated as nullable reference types are a union with `null`: `List<string?>` gives `"items": ["null", "string"]`, `Dictionary<string, string?>` gives `"values": ["null", "string"]` (also `string?[]`, nested collections). 3.x and the previews gave `"string"`, so a `null` item threw (3.x wrote `""`). The schema of such a class changes; Schema Registry in the default `BACKWARD` mode accepts it, `FORWARD` and `FULL` do not. Without `#nullable enable` nothing changes. A type parameter of a generic class (`List<T>` in `Page<T>`) stays as it was
- `[DefaultValue(Color.Red)]` on an enum member gives `"default": "Red"`, the symbol, as the specification requires; 3.x wrote the number, which Java and Confluent Schema Registry reject
- `Avro2Json` does not use `JsonConvert.DefaultSettings` of the application: the names are the names in the schema, and the JSON is compact with ISO 8601 dates, whatever the contract resolver, `Formatting`, date settings or converters set there. 3.x and the previews applied those settings, e.g. a camel-case contract resolver renamed the fields
- `DeserializeHeadless<List<T>>` reads all rows, not only the first one (#69)
- Read objects are created with their public parameterless constructor, like in System.Text.Json; a field missing in the data keeps its initialiser
- `DateTimeOffset` is written as its UTC instant instead of the wall-clock time
- Snappy blocks include the CRC-32 required by the specification
- Generated schemas differ slightly, data stays compatible: names of generic records (#159), `time-micros`, non-public `[DataMember]` members (#51), `IDictionary<string, T>` / `IReadOnlyDictionary<string, T>` members are maps instead of arrays of key-value records
- Multidimensional arrays (`int[,]`) throw; use jagged arrays (#26)
- Numbers read into `dynamic` follow the reader schema type
- Unknown `logicalType` is read as its underlying type
- `SchemaConvert` removed; use `AvroSchema` (#73)
- `DeserializeHeadless(bytes, writeSchema, readSchema, type)` reads all the rows into a collection, like the generic overload, instead of the first one (#69)
- Removed the overloads of the previews taking the schema as text: `Avro2JsonEncoding(avro, string)`, `JsonEncoding2Avro(json, string)`, `JsonEncoding2Avro(json, string, CodecType)`. Use `AvroSchema.Parse`
- Removed `JsonEncoding2Avro(json, AvroSchema, CodecType)` of the previews; the codec goes in `AvroConvertOptions`
- Overloads of the previews merged into one with optional parameters: `DeserializeHeadless<T>(bytes, writer)`, `DeserializeHeadless<T>(bytes, writer, reader)`, `JsonEncoding2Avro(json, schema)`. Calls compile as before, but code compiled against a preview must be rebuilt
- Overloads taking a `Type` known at runtime (`Deserialize(bytes, type)`, `DeserializeHeadless(bytes, …, type)`) throw the exception of the call, not a `TargetInvocationException` around it
- `Serialize(IAsyncEnumerable<T>, stream, null)` of the previews is ambiguous next to the overload taking an `AvroSchema`; leave the `null` out or name it (`options: null`)
- Reflection-based methods marked `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`
- SolTechnology.Avro.Kafka rewritten on Confluent.SchemaRegistry 2.16: Confluent wire format, schema evolution, references, subject name strategies. Removed `KafkaConsumer`, `KafkaPropNames`, `ConfluentSchema`
- SolTechnology.Avro.Http follows System.Net.Http.Json (`GetFromAvroAsync`, `PostAsAvroAsync`, `ReadFromAvroAsync`…), Minimal API results and streaming; old names are `[Obsolete]`
- `GenerateModel`: `date` → `DateOnly`, `time-millis` / `time-micros` → `TimeOnly`; the code starts with `using` directives (and `#nullable enable annotations` when it has nullable types) and has `[DataContract]` / `[DataMember]`, `[AvroType]`, `[AvroDecimal]` and `[AvroFixed]` where needed, so the classes give back the same schema (exceptions in [Generate model](Documentation.Schema.md#generate-model)). A union lists every branch in `[AvroUnion]`, not only the records; the default of an enum is its symbol (`[DefaultValue("Paid")]`)

Changes:
- Serialization and deserialization compiled per schema and type: several times faster, a fraction of allocations
- Added SolTechnology.Avro.Cli (`avroconvert`): `induce`, `compile`, `tojson`, `getschema`, `canonical`, `fingerprint`, `compat`, `registry`
- Added streaming: `Serialize<T>(IEnumerable<T> / IAsyncEnumerable<T>, Stream)`, `Deserialize<T>(Stream)` returning `IAsyncEnumerable<T>`, output to `IBufferWriter<byte>`
- Smaller public API: one way to give a schema (`AvroSchema`) and a codec (`AvroConvertOptions`). The overloads taking the schema as text, a `CodecType` or a file path are hidden from IntelliSense and keep working. To be removed in the next major version
- `DeserializeHeadless` with `AvroSchema` takes everything at one call: reader schema, options, `numberOfRows`, a type known at runtime. Without a reader schema it uses the schema of `T` built with the given options
- Added `Json2Avro(json, options)`
- Added `AvroSchema`: canonical form, fingerprints, `Compare` and `CheckCompatibility` (#73)
- Added `AvroSchema.Parse(json, referencedSchemas)` for schemas using types from other schemas
- Added `Avro2JsonEncoding` / `JsonEncoding2Avro` – JSON as defined by the Avro specification (#156)
- Added logical types `timestamp-nanos`, `local-timestamp-*`, `big-decimal`, `uuid` on `fixed`, and `AvroConvertOptions.DateTimeRepresentation` (#153)
- Added `zstandard` codec
- Added support for recursive types
- Added support for `ExpandoObject` (#112) and `DeserializeHeadless<dynamic>` (#107)
- Added support for union of records read into `object` / `dynamic` (#174)
- Added support for map keys convertible from string (e.g. `Dictionary<Uri, T>`)
- Added support for dictionary types other than `Dictionary<,>`: `IReadOnlyDictionary<,>`, `SortedDictionary<,>`, `ReadOnlyDictionary<,>` and classes deriving from `Dictionary<,>`
- Added `Deserialize(byte[], Type, options)` and `DeserializeHeadless<T>(ReadOnlySpan<byte>, writer, reader, options)`
- Added reading a record into a dictionary of objects, with nested records as dictionaries (#197)
- Added `metadata` argument of `Serialize` with a schema, and `AvroConvert.GetMetadata`: own keys in the container header (#196). `Merge` keeps the keys all the files agree on
- Added `AvroSchema.FromItems`: the schema of the entries of a file, completed from all of them where they hold `object`
- Added `Serialize` overloads taking an `AvroSchema`: a container written with a given schema
- Added `AvroSchema.FromValue`: the schema of a value, for headless data with `object` values
- Reads files written by Apache.Avro for C# with a logical type on a `fixed` or an `enum` (e.g. `decimal` on `fixed`), whose header has the logical type around the definition
- `[AvroFixed(size, name, namespace)]` is public: a `byte[]` member as a named `fixed`, `["null", fixed]` for `byte[]?`
- `[AvroType]` next to `[AvroUnion]` applies to the branch it fits (`timestamp-millis` to `DateTime`, not to a `string` next to it)
- An enum value in a union with a string branch goes to the enum branch; read into `object` with `[AvroUnion]` it is the enum again
- The classes and enums of `[AvroUnion]` are also found by their `[DataContract(Name)]`: they were read back as `ExpandoObject` or text
- `GenerateModel` handles `fixed` (also referred to by name), unions of any branches, arrays of arrays and of unions, maps of records defined in place, `error` types, C# keywords as names, string / `bool?` / enum / `NaN` defaults, multi-line docs, records of one name in two namespaces and a file of several schemas, where it generated code that did not compile or lost union branches
- `string?` members of a class nested in another one are a union with `null` when the nullable context is on the outer class; they were taken as not nullable
- `ExpandoObject` and `Json2Avro` reuse the compiled plan of a schema they have seen, instead of compiling one for every call
- JSON objects and `ExpandoObject`s with one key holding different fields keep them: the records get names of their own. Items of a list with different nested objects keep all their fields
- `Dictionary<string, object>`, `object` members, `List<object>` and `ExpandoObject` members take their schema from the values being written. A string in them threw, and a class lost its fields without an error. `List<object>` is written as one entry per item, like other lists
- A value written against the empty schema of `object` throws instead of losing its fields (`SerializeHeadless(value, typeof(T))`)
- A union takes the branch of exactly the value's type first: a `double` next to a `long` branch is not truncated, a `DateTime` next to a `date` branch keeps its time, a class goes to its own record, not to the first that looks like it
- A dictionary, `ExpandoObject` or `JObject` written to a union of records goes to the record that takes all its keys, and throws when there is none
- Fields of a dictionary written as a record are found by name, then by alias
- `Serialize<object>(items, stream)` writes items of different types as a union; only an interface or an abstract class needs one runtime type
- Unions of logical types (e.g. `date` and `timestamp-micros`) and of records of one name in different namespaces are parsed and read
- A named type used in several places of a schema is defined once; a different type of a name taken already (e.g. nested enums `Order.Status` and `Shipment.Status`) gets the name with a number (`Status_2`) instead of a reference to the other type
- Fix for `Avro2Json` of a union whose branches have one short name: it read the first of them
- `Json2Avro` and `InferFromJson`: different values in a JSON array give a union; a number next to text was written as text
- Text that looks like a date stays text in schema attributes and defaults
- A reference cycle, also in an `ExpandoObject`, throws instead of ending the process or never returning
- Schemas keep the attributes the specification does not define (e.g. `field-id`), logical types Avro does not know, and enum aliases. The canonical form and fingerprints are not affected
- Optional licence declaration: `AvroConvert.License` or `AVROCONVERT_LICENSE`. SolTechnology.DataLake includes AvroConvert under its own licence and shows its own notice instead
- Writing with a schema field missing in the type writes the field's default; the same for a key missing in a dictionary, `ExpandoObject` or `JObject` written as a record
- Schema resolution promotes `string` ↔ `bytes`; field `order` and enum `default` are preserved
- Dates and times in a `string` field are read and written as ISO 8601 text (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`), like Chr.Avro writes them by default
- `DateTime` reads and writes `date`, `TimeSpan` reads and writes `time-millis` / `time-micros` (the types of Apache.Avro `avrogen` classes)
- `Json2Avro` and `InferFromJson` look at every array element, not only the first one
- A failed serialization does not leave a data block that looks complete
- Fix for a recursive record without a matching class read into `dynamic` or `ExpandoObject`: it ended the process with a stack overflow
- Data nested too deep and schemas without an end throw instead of ending the process with a stack overflow
- `Avro2Json` of data nested deep enough to be read but not written as JSON (about 1,200 levels on a 1 MB stack) throws instead of ending the process: the JSON is written with a stack check at every level
- Fix for `OpenDeserializer<ExpandoObject>` returning objects without fields
- Fix for `GetSchema(path)` on read-only files and files open elsewhere
- Fix for `Merge` losing data after the first block
- Fix for `Merge` of objects written with another schema than `T`: the result keeps the schema they were written with; objects written with different schemas throw instead of giving unreadable data
- Fix for `Merge` of files written with `CollectionMode = SingleArray`
- Fix for field and record aliases on read: a renamed field (`[DataMember(Name)]` with the old name in the data) and a union record matched by alias
- Fix for a union of records on read: the branch is chosen by name or alias first, then by the first record the data resolves into, the same rule as outside a union
- Fix for nullable struct records (`Point?`)
- Fix for `long` read into a `timestamp-*` field of the reader schema
- Fix for `IncludeOnlyDataContractMembers` with `DateOnly`, `TimeOnly` and `TimeSpan` members
- Fix for nullable members holding a generic record (with a schema given as JSON), a dictionary with non-string keys, or a record written under another name (anonymous type, `ExpandoObject`)
- Fix for `GenerateModel` output differing between operating systems
- Fix for `decimal` on `fixed`
- Fix for defaults of logical type fields
- Fix for one-element collection and nested collections (`List<List<T>>`)
- Fix for reading an empty array with `OpenDeserializer`
- Fix for `NamingPolicy` on deserialization
- Fix for `MemoryStream` not disposed in `SerializeHeadless` (#163)
- Dependencies: Newtonsoft.Json, IronSnappy, ZstdSharp.Port

\
**v. 3.4.17 (14.07.26)**
- Nullable handling in Union schemas
- Fix for records to skip additional fields

**v. 3.4.16 (18.09.25)**
- Add number handling options to round/truncate decimals

**v. 3.4.15 (31.03.25)**
- Fix for namespace in Union resolution

**v. 3.4.14 (19.03.25)**
- Different approach to Union type support; exposing AvroUnionAttribute

\
**v. 3.4.13 (18.03.25)**
- Added support for Union type for serialization and deserialization of different classes being represented as single object in the c# model 

\
**v. 3.4.12 (07.03.25)**
- Fix naming policy for enums

\
**v. 3.4.11 (06.03.25)**
- Add naming policy option for customizing type and member names

\
**v. 3.4.10 (21.10.24)**
- Fix support for nullable types for AvroType attribute

\
**v. 3.4.9 (18.09.24)**
- Update EnumSchema to handle Enums with duplicate values

\
**v. 3.4.8 (08.05.24)**
- Another approach to handle serialization of multiple dictionaries, as previous introduced breaking change

\
**v. 3.4.7 (26.04.24)**
- Fix for dynamic deserialization to collection
- Fix for Avro2Json to read correctly multiple data blocks
- Fix for serialization of class with multiple dictionaries
- Added support for AvroTypeAttribute - allows applying custom avro schema representation for chosen field/property

\
**v. 3.4.6 (04.03.24)**
- Fix for missing decimal schema for default value resolver

\
**v. 3.4.5 (15.02.24)**
- Add support for IEnumerable objects and properties

\
**v. 3.4.4 (16.01.24)**
- Add support for EnumMember attribute 

\
**v. 3.4.3 (09.12.23)**
- Fix for caching of skippers during deserialization
- Fix for deserialization of enums marked with flags 

\
**v. 3.4.2 (01.12.23)**
- Added support for get-only properties for deserialization
- Further performance improvements

\
**v. 3.4.1 (11.11.23)**
- Significant performance improvements
- BuildSchema supports Guid default value
- AvroConvertOnline is back online (moved to https://soltechnology.dev/avroconvert/online/ in 4.0)

\
**v. 3.4.0 (18.09.23)**
- Added support for custom IAvroConverter - allows applying custom implementation of serialization/deserialization for chosen type
- Added support for deserialization to dynamic type (default flow only)

\
**v. 3.3.7 (07.08.23)**
- Increased default precision of time types to microseconds (tick * 10)
- Added Deserialize overload taking ReadOnlySpan<byte> as an argument

\
**v. 3.3.6 (29.05.23)**
- Created new Tests engine which discovered few not supported cases :)
- Allowed default value for Enum fields
- Changed GenerateClass default value representation
- Extended GenerateModel to generalize complex type build of different types (ex. null, string, record) to object
- Avro2Json now supports: DefaultValue attribute and array of primitive values, dictionary with object keys
- Fix for reading default value for nullable enums
- Improved deserialization time by skipping union matching for primitives

\
**v. 3.3.5 (11.05.23)**
- Fix for GenerateModel to apply culture specific decimal separator
- Short and ushort int16 are supported for serialization

\
**v. 3.3.4 (03.03.23)**
- Allowed any item to be deserialized into object (it's for supporting map of multiple values)
- Added support for serialization using ExpandoObject
- Extended GenerateModel to support map of union type

\
**v. 3.3.3 (28.02.23)**
- Added support for proto generated models
- Added support for ushort, uint, ulong, byte, sbyte, char

\
**v. 3.3.2 (06.02.23)**
- Extended interfaces of Headless methods (including non-generic and Type as parameter)
- Added support for serialization using anonymous object

\
**v. 3.3.1 (27.01.23)**
- Fix of SerializeHeadless invocation

\
**v. 3.3.0 (11.10.22)**
- Added support for short (int16) C# type
- Allowed conversion for non-matching read and write number C# types (ex: [int] can be read as [long])
- Added SchemaConvert.GenerateFromJson feature
- Added dynamic Json2Avro feature
- Decreased serialization time and memory allocation by about 20%
- Added support for DateOnly (Date) and TimeOnly (Duration) C# types

\
**v. 3.2.9 (22.06.22)**
- Added support for union of Records
- Fixed Avro2Json timestamp serialization and deserialization
- Added Generic Json2Avro feature as foundation for dynamic one

\
**v. 3.2.8 (01.06.22)**
- Added support for Fixed and Map in GenerateModel

\
**v. 3.2.7 (20.05.22)**
- Added support for files containing only Avro Schema for: Deserialization, DeserializationByLine, Merge, GetSchema, Avro2Json

\
**v. 3.2.6 (16.05.22)**
- Added support for class with parameterized constructor only in: Schema Generation, Serialization, Deserialization

\
**v. 3.2.5 (13.04.22)**
- Allowed schema generation for Abstract class and Interface properties

\
**v. 3.2.4 (10.04.22)**
- Add codegen support for Default and Doc Fields
- Fixed bug with missing commas on Enum generation

\
**v. 3.2.3 (29.10.21)**
- Split fullName field into name and namespace in GenerateSchema to match documentation

\
**v. 3.2.2 (02.10.21)**
- Fixed GenerateModel problem when field is array or nullable

\
**v. 3.2.1 (29.09.21)**
- Added enums to GenerateModel output
- Fixed bug with reading only first data block
- Added namespaces in GenerateModel output, when multiple classes are of the same name
- Fixed bug with reading single item by DeserializeByLine

\
**v. 3.2.0 (14.09.21)**
- Added 'Generate C# Model' feature
- Fix multithreaded serialization


\
**v. 3.1.5 (30.08.21)**
- Fix serialization of inheriting class (from class)

\
**v. 3.1.4 (12.08.21)**
- Allow data members to be ignored with IgnoredDataMember attribute

\
**v. 3.1.3 (02.08.21)**
- Significantly improve both serialization and deserialization time
- Reduced memory allocation
- Fixed Avro attributes handling in headless deserialization

\
**v. 3.1.2 (24.06.21)**
- Allow generation of nullable reference types by using C# 8 nullable annotation

\
**v. 3.1.1 (14.06.21)**
- Ignore missing fields when deserializing - This use case for this is when the producer has added a field (and the schema has been updated in the Schema Registry), but consumers are still using classes that don't yet have the field.

\
**v. 3.1.0 (15.05.21)**
- Added merge feature

\
**v. 3.0.1 (02.04.21)**
- Fixed bug with deserialization of nullable enum field

\
**v. 3.0.0 (26.03.21)**
- Added support for Avro Logical Types !BREAKING CHANGE!
- Improved deserialization time by removing duplicated schema implementation
- Added support for Avro Doc attribute (.NET DescriptionAttribute)

\
**v. 2.7.1 (05.02.21)**
- Replaced own Snappy implementation with IronSnappy (to support all platforms)
- Added support for C# 9 record type

\
**v. 2.7.0 (27.12.20)**
- Added support for Brotli encoding
- Improved Deserialization performance
- Reduced Memory Allocation during Deserialization
- Changed Benchmark approach - switched to BenchmarkDotNet

\
**v. 2.6.3 (25.10.20)**
- Fixed header metadata extraction - allows getting non-existing keys (to allow reading files without codec metadata)

\
**v. 2.6.2 (23.10.20)**
- Fixed codec parsing - returns NullCodec by default

\
**v. 2.6.1 (25.07.20)**
- Added support for nullable DateTime
- Added support for nullable DateTimeOffset

\
**v. 2.6.0 (22.07.20)**
- Added deserializer which allows reading large collection of AVRO objects one by one

\
**v. 2.5.1 (11.06.20)**
- Properties are mapped to model fields in case insensitive convention

\
**v. 2.5.0 (15.05.20)**
- Added Avro2Json feature
- Added support for a number of types implementing IEnumerable
- Improved deserialization time

\
**v. 2.4.1 (14.04.20)**
- Improved performance of deserialization
- Adjusted generated schema to match Apache.Avro

\
**v. 2.4.0 (25.03.20)**
- Improved performance of AvroConvert
- Removed reference to AutoMapper
- Added performance benchmark (comparison to Newtonsoft.Json and Apache.Avro)
- Added Headless Serialization and Deserialization:
- Resolver doesn't generate schema by its own
- Requires providing schema for serialization and deserialization. Schemas have to be exactly the same
- Solution with higher performance

\
**v. 2.3.0 (19.03.20)**
- Changed license to CC BY-NC-SA 3.0
- Added support for complex types including enums
- Improved Schema compatibility with Apache.Avro

\
**v. 2.2.2 (07.02.20)**
- Deployment fixes

\
**v. 2.2.1 (07.02.20)**
- Added support for enum schema during serialization and deserialization

\
**v. 2.2.0 (27.01.20)**
- Added support for GZip Codec
- Created interface for library - only AvroConvert methods are public
- Removed reference to Snappy.NET
- Ported Snappy.Standard
- Added dynamic Deserialize method

\
**v. 2.1.0 (16.01.20)**
- Added support for Snappy Codec
- Added possibility to choose Snappy compression during serialization
- Added possibility to choose Deflate compression during serialization

\
**v. 2.0.0 (04.01.20)**
- Improved performance during serialization
- Added support for ConcurentBagClass
- Added support for Dictionary properties
- Snappy encoding is not supported

\
**v. 1.8.1 (09.12.19)**
- Fixed bug during deserialization of object containing property with empty list

\
**v. 1.8.0 (30.08.19)**
- Added possibility to define DefaultValue for members
- DefaultValue is visible in the schema header
- DefaultValue is taken into consideration in deserialization when member value is null
- Added possibility to generate schema containing only members decorated with DataMember attribute
- Added possibility to provide schema used for Deserialization

\
**v. 1.7.0 (22.08.19)**
- Schema build optimizations
- Fixed problem with aliases in result
- Added support for Hashsets
- Added support for DateTimeOffset type

\
**v. 1.6.0 (22.07.19)**
- Added full support for public fields in classes (in addition to properties)

\
**v. 1.5.1 (11.07.19)**
- Refactored deserialization of Guid type

\
**v. 1.5.0 (11.07.19)**
- Added support for DateTime type

\
**v. 1.4.0 (17.06.19)**
- Added support for Avro Attributes
- Removed reflection schema generation implementation, replaced by original, created by Microsoft
- Possibility to read Avro objects into classes containing only part of the properties

\
**v. 1.3.0 (04.06.19)**
- Changed project type to .NET Standard 2.0
- Removed reference to Microsoft.Hadoop.Avro-Core

\
**v. 1.2.0 (03.06.19)**
- Refactor of writers
- Refactor of models
- Support for Dictionaries with non-string keys (not matching Avro Map class)

\
**v. 1.1.0 (28.05.19)**
- Split encoders into separate, depending on the encoding type
- Added support for Dictionaries of complex types

\
**v. 1.0.0 (24.04.19) VERSION ROLLED BACK**
- Disabled parallelization in tests
- Improved reading strings

\
**v. 0.7.0 (22.04.19)**
- Added support of null records inside Array
- Finally fixed reading of null properties

\
**v. 0.6.3 (19.04.19)**
- Fixed reading null properties

\
**v. 0.6.2 (19.04.19)**
- Fixed reading null properties

\
**v. 0.6.1 (19.04.19)**
- Fixed reading null properties

\
**v. 0.6.0 (19.04.19)**
- Added support of Guid type
- Fixed bug with skipping properties during schema generation

\
**v. 0.5.0 (18.04.19)**
- Added support for serialization of a List type
- Added support for serialization of an Array type

\
**v. 0.3.0 (11.04.19)**
- Changed namespace to 'Avro'

\
**v. 0.2.1 (11.04.19)**
- Fixed reading information about Codec

\
**v. 0.2.0 (11.04.19)**
- Added License information
- Added deserialization of Array type

\
**v. 0.1.0 (10.04.19)**
- Added reading of the structure of Avro files
- Added support for String type
- Added support for primitive types
- Extracted schema read logic to separate place
- Added simple unit tests for an example file
- Added deserialization of Avro files into key-value pairs
- Added serialization to Avro mechanism
- Added reflection module creating in-memory types, which mimics objects to serialize
- Added support of nullable types
- Added AutoMapper reference - from key-value pairs into the desired object
- Added Component tests
- Added deserialization of List type

\
**v. 0.0.0 (24.03.19)**
\
START OF THE PROJECT
