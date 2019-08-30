**v. 0.0.0 (24.03.19)**
</br> 
START OF THE PROJECT
</br>
</br>
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
</br>
</br>
**v. 0.2.0 (11.04.19)**
- Added License information
- Added deserialization of Array type
</br>
</br>
**v. 0.2.1 (11.04.19)**
- Fixed reading information about Codec
</br>
</br>
**v. 0.3.0 (11.04.19)**
- Changed namespace to 'Avro'
</br>
</br>
**v. 0.5.0 (18.04.19)**
- Added support for serialization of a List type
- Added support for serialization of a Array type
</br>
</br>
**v. 0.6.0 (19.04.19)**
- Added support of Guid type
- Fixed bug with skipping properties during schema generation
</br>
</br>
**v. 0.6.1 (19.04.19)**
- Fixed reading null properties
</br>
</br>
**v. 0.6.2 (19.04.19)**
- Fixed reading null properties
</br>
</br>
**v. 0.6.3 (19.04.19)**
- Fixed reading null properties
</br>
</br>
**v. 0.7.0 (22.04.19)**
- Added support of null records inside Array
- Finally fixed reading of null properties
</br>
</br>
**v. 1.0.0 (24.04.19) VERSION ROLLED BACK**
- Disabled parallelization in tests
- Improved reading strings
</br>
</br>
**v. 0.8.0 (01.05.19)**
- Added support for Dictionary type
- Refactor of reflection schema generation
</br>
</br>
**v. 0.8.1 (09.05.19)**
- Added support for null Strings
- Unified namings of encodings logic
</br>
</br>
**v. 1.1.0 (28.05.19)**
- Splitted encoders into separate, depending of the encoding type
- Added support for Dictionaries of complex types
 </br>
</br>
**v. 1.2.0 (03.06.19)**
- Refactor of writers
- Refactor of models
- Support for Dictinaries with non-string keys (not matching Avro Map class)
 </br>
</br>
**v. 1.3.0 (04.06.19)**
- Changed project type to .NET Standard 2.0
- Removed reference to Microsoft.Hadoop.Avro-Core
 </br>
</br>
**v. 1.4.0 (17.06.19)**
- Added support for Avro Attributes
- Removed reflection schema generation implementation, replaced by original, created by Microsoft
- Possibility to read Avro objects into classes containing only part of the properies
 </br>
</br>
**v. 1.5.0 (11.07.19)**
- Added support for DateTime type
 </br>
</br>
**v. 1.5.1 (11.07.19)**
- Refactored deserialization of Guid type
</br>
</br>
**v. 1.6.0 (22.07.19)**
- Added full support for public fields in classes (in addition to properties)
</br>
</br>
**v. 1.7.0 (22.08.19)**
- Schema build optimizations
- Fixed problem with aliases in result
- Added support for Hashsets
- Added support for DateTimeOffset type
</br>
</br>
**v. 1.8.0 (30.08.19)**
- Added possibility to define DefaultValue for members
- DefaultValue is visible in the schema header
- DefaultValue is taken into consideration in deserialization when member value is null
- Added possiblity to generate schema containing only members decorated with DataMember attribute
- Added possibility to provide schema used for Deserialization