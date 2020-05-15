
<p align="center">
    <img alt="SolTechnology-logo" src="./docs/logo.png" width="200">
</p>

<h2 align="center">
  AvroConvert
</h2>

<p align="center">
 <a> Rapid Avro serializer for C# .NET </a>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/AvroConvert"><img src="https://img.shields.io/badge/Nuget-v2.5.0-blue?logo=nuget"></a>
  <a href="https://github.com/AdrianStrugala/AvroConvert"><img src="https://img.shields.io/badge/Downloads-7k-blue?logo=github"></a>
  <a href=""><img src="https://img.shields.io/appveyor/build/AdrianStrugala/AvroConvert"></a>
</p>


## Docs

**Avro format combines readability of JSON and compression of binary data serialization.**

[Introduction article](https://xabe.net/why-avro-api-is-the-best-choice/)

[Apache Wiki](https://cwiki.apache.org/confluence/display/AVRO/Index)

[General information](http://avro.apache.org/)

[Documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md)

## Benefits

The main purpose of the project was to enhance communication between microservices. Replacing JSON with Avro brought three main benefits:
* Decreased the communication time between microservices
* Reduced the network traffic by about 30%
* Increased communication security - the data was not visible in plain JSON text

| Converter               | Request time [ms]     | Compressed size [kB] |
|-------------------------|-----------------------|----------------------|
| Json                    | 1076                  | 9945                 |
| Avro (default)          | 509                   | 2435                 |
| Avro (Headless)         | 465                   | 2434                 |
| Avro (Deflate encoding) | 453                   | 206                  |

In the purpose of introducing Avro API, I've written an article, which you can read here: https://xabe.net/why-avro-api-is-the-best-choice/
\
It contains also description of the format, detailed results of the benchmarks and implementation details.

**Conclusion:** <br>
Using Avro for communication between your services significantly reduces communication time and network traffic. Additionally choosing encoding (compression algorithm) can improve the results even further.

## Features

* Serialization and deserialization data in Avro format
* Extended support for C# data structures: Dictionary, List, DateTime and many others
* Support for codecs: deflate, snappy, gzip
* Support for Attributes: DataContract, DataMember, DefaultValue, NullableSchema
* "Headless" - serialization and deserialization based on provided schema

[Full Changelog](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/CHANGELOG.md)

## Code samples

```csharp
//Serialization
 byte[] avroObject = AvroConvert.Serialize(object yourObject);
```

```csharp
//Deserialization
CustomClass deserializedObject = AvroConvert.Deserialize<CustomClass>(byte[] avroObject);
```

```csharp
//Generate Avro schema

//Model
public class SimpleTestClass
{
	public string justSomeProperty { get; set; }

	public long andLongProperty { get; set; }
}


//Action
string schemaInJsonFormat = AvroConvert.GenerateSchema(typeof(SimpleTestClass));


//Produces following schema:
"{"type":"record","name":"AvroConvert.SimpleTestClass","fields":[{"name":"justSomeProperty","type":"string"},{"name":"andLongProperty","type":"long"}]}"
```

```csharp
//Read schema from Avro file
string schemaInJsonFormat = AvroConvert.GetSchema(byte[] avroObject)
```

[Full Documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md)


## Related packages  

[![Nuget](https://img.shields.io/badge/Soltechnology.Avro.Http-v0.2.0-blue?logo=nuget)](https://www.nuget.org/packages/SolTechnology.Avro.Http/) - Library containing functionalities, which enable communication between microservices via Http using Avro data format

## License  

AvroConvert is licensed under [Attribution-NonCommercial-ShareAlike 3.0 Unported (CC BY-NC-SA 3.0)](https://creativecommons.org/licenses/by-nc-sa/3.0/) - see [License](LICENSE.md) for details. For commercial purposes purchase AvroConvert on website - [Xabe](https://xabe.net/product/avroconvert/)


## Contribution

We want to improve AvroConvert as much as possible. If you have any idea, found next possible feature, optimization opportunity or better way for integration, leave a comment or pull request. 


