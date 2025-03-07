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
- AvroConvertOnline is back online

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
