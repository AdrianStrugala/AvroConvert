**v. 0.0.0 (24.03.19)**
\
START OF THE PROJECT

\
**v. 0.1.0 (10.04.19)**
- Added reading of structure of Avro files
- Added support for String type
- Added support for primitive types
- Extracted schema read logic to separate place
- Added simple unit tests for example file
- Added deserialization of Avro files into key-value pairs
- Added serialization to Avro mechanism
- Added reflection module creating in-memory types, which mimics objects to serialize
- Added support of nullable types
- Added AutoMapper reference - from key-value pairs into desired object
- Added Component tests
- Added deserialization of List type

\
**v. 0.2.0 (11.04.19)**
- Added License information
- Added deserialization of Array type

\
**v. 0.2.1 (11.04.19)**
- Fixed reading information about Codec

\
**v. 0.3.0 (11.04.19)**
- Changed namespace to 'Avro'

\
**v. 0.5.0 (18.04.19)**
- Added support for serialization of a List type
- Added support for serialization of a Array type

\
**v. 0.6.0 (19.04.19)**
- Added support of Guid type
- Fixed bug with skipping properties during schema generation

\
**v. 0.6.1 (19.04.19)**
- Fixed reading null properties

\
**v. 0.6.2 (19.04.19)**
- Fixed reading null properties
\
\
**v. 0.6.3 (19.04.19)**
- Fixed reading null properties

\
**v. 0.7.0 (22.04.19)**
- Added support of null records inside Array
- Finally fixed reading of null properties

\
**v. 1.0.0 (24.04.19) VERSION ROLLED BACK**
- Disabled parallelization in tests
- Improved reading strings

\
**v. 0.8.0 (01.05.19)**
- Added support for Dictionary type
- Refactor of reflection schema generation

\
**v. 0.8.1 (09.05.19)**
- Added support for null Strings
- Unified namings of encodings logic

\
**v. 1.1.0 (28.05.19)**
- Splitted encoders into separate, depending of the encoding type
- Added support for Dictionaries of complex types

\
**v. 1.2.0 (03.06.19)**
- Refactor of writers
- Refactor of models
- Support for Dictionaries with non-string keys (not matching Avro Map class)

\
**v. 1.3.0 (04.06.19)**
- Changed project type to .NET Standard 2.0
- Removed reference to Microsoft.Hadoop.Avro-Core

\
**v. 1.4.0 (17.06.19)**
- Added support for Avro Attributes
- Removed reflection schema generation implementation, replaced by original, created by Microsoft
- Possibility to read Avro objects into classes containing only part of the properies

\
**v. 1.5.0 (11.07.19)**
- Added support for DateTime type

\
**v. 1.5.1 (11.07.19)**
- Refactored deserialization of Guid type

\
**v. 1.6.0 (22.07.19)**
- Added full support for public fields in classes (in addition to properties)

\
**v. 1.7.0 (22.08.19)**
- Schema build optimizations
- Fixed problem with aliases in result
- Added support for Hashsets
- Added support for DateTimeOffset type

\
**v. 1.8.0 (30.08.19)**
- Added possibility to define DefaultValue for members
- DefaultValue is visible in the schema header
- DefaultValue is taken into consideration in deserialization when member value is null
- Added possiblity to generate schema containing only members decorated with DataMember attribute
- Added possibility to provide schema used for Deserialization

\
**v. 1.8.1 (09.12.19)**
- Fixed bug during deserializaion of object conaining property with empty list

\
**v. 2.0.0 (04.01.20)**
- Improved performance during serialization
- Added support for ConcurentBagClass
- Added support for Dictionary properties
- Snappy encoding is not supported

\
**v. 2.1.0 (16.01.20)**
- Added support for Snappy Codec
- Added possibility to choose Snappy compression during serialization
- Added possibility to choose Deflate compression during serialization

\
**v. 2.2.0 (27.01.20)**
- Added support for GZip Codec
- Created interface for library - only AvroConvert methods are public
- Removed reference to Snappy.NET
- Ported Snappy.Standard
- Added dynamic Deserialize method

\
**v. 2.2.1 (07.02.20)**
- Added support for enum schema during serialization and deserialization

\
**v. 2.2.2 (07.02.20)**
- Deployment fixes

\
**v. 2.3.0 (19.03.20)**
- Changed license to CC BY-NC-SA 3.0
- Added support for complex types including enums
- Improved Schema compatibility with Apache.Avro

\
**v. 2.4.0 (25.03.20)**
- Improved performance of AvroConvert
- Removed reference to AutoMapper
- Added performance benchmark (comparison to Newtonsoft.Json and Apache.Avro)
- Added Headless Serialization and Deserialization:
  - Resolver doesn't generate schema by it's own
  - Requires providing schema for serialization and deserialization. Schemas have to be *exactly* the same
  - Solution with higher performance

\
**v. 2.4.1 (14.04.20)**
- Improved performance of deserialization
- Adjusted generated schema to match Apache.Avro

\
**v. 2.5.0 (15.05.20)**
- Added Avro2Json feature
- Added support for number of types implementing IEnumerable
- Improved deserialization time

\
**v. 2.5.1 (11.06.20)**
- Properties are mapped to model fields in case insensitive convention 

\
**v. 2.6.0 (22.07.20)**
- Added deserializer which allows to read large collection of AVRO objects one by one

\
**v. 2.6.1 (25.07.20)**
- Added support for nullable DateTime
- Added support for nullable DateTimeOffset