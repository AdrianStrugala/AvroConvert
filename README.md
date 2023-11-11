
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
 <a href="https://www.nuget.org/packages/AvroConvert"><img src="https://img.shields.io/badge/Nuget-v3.4.1-blue?logo=nuget"></a>
 <a href="https://adrianstrugala.github.io/AvroConvert/"><img src="https://img.shields.io/badge/Downloads-530k-blue?logo=github"></a>
 <a href="https://github.com/AdrianStrugala/AvroConvert/actions/workflows/build&test.yml"><img src="https://github.com/AdrianStrugala/AvroConvert/actions/workflows/build&test.yml/badge.svg"></a>

</p>

## Docs

**Avro format combines readability of JSON and data compression of binary serialization.**

[Apache Wiki](https://cwiki.apache.org/confluence/display/AVRO/Index)

[Apache Avro format documentation](http://avro.apache.org/)

[AvroConvert Documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md)

[First steps with Avro in the .NET article](https://www.c-sharpcorner.com/blogs/avro-rest-api-as-the-evolution-of-json-based-communication-between-mic)

[Benchmark and Avro API article](https://www.c-sharpcorner.com/blogs/avro-rest-api-as-the-evolution-of-json-based-communication-between-mic)



## Benefits

Introducing Avro to the projects brings three main benefits:
* Reduction of data size and storage cost
* Decrease of the communication time and the network traffic between microservices
* Increased security - the data is not visible in plain text format


## Features
|                                                               | AvroConvert                                | Apache.Avro | Newtonsoft.Json |
|---------------------------------------------------------------|:------------------------------------------:|:-----------:|:---------------:|
| Rapid serialization                                            |                      ✔️                     |      ✔️      |        ✔️        |
| Easy to use                                                   |                      ✔️                     |      ❌      |        ✔️        |
| Built-in compression                                          |                      ✔️                     |      ✔️      |        ❌        |
| Low memory allocation                                         |                      ✔️                     |      ✔️      |        ✔️        |
| Support for C# data structures: Dictionary, List, DateTime... |                      ✔️                     |      ❌      |        ✔️        |
| Support for compression codecs                                | Deflate<br/>  Snappy<br/> GZip<br/> Brotli |   Deflate   |        ❌        |
| Readable schema of data structure                                      |                      ✔️                     |      ✔️      |        ✔️        |
| Data encryption                                       |                      ✔️                     |      ✔️      |        ❌        |

[Full Changelog](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/CHANGELOG.md)



## Benchmark

Results of BenchmarkDotNet:

|Converter     | Request Time [ms] | Allocated Memory [MB] | Compressed Size [kB] |
|------------- |------------------:|----------------------:|---------------------:|
| Json         |       672.3       |          52.23        |         6044         |
| Avro         |       384.7       |          76.58        |         2623         |
| Json_Gzip    |       264.1       |          88.32        |          514         |
| Avro_Gzip    |       181.2       |          75.05        |          104         |
| Json_Brotli  |       222.5       |          86.15        |           31         |
| Avro_Brotli  |       193.5       |          74.75        |           31         |


Article describing Avro format specification and benchmark methodology: https://www.c-sharpcorner.com/blogs/avro-rest-api-as-the-evolution-of-json-based-communication-between-mic

**Conclusion:** <br>
Using Avro for communication between your services significantly reduces data size and network traffic. Additionally choosing encoding (compression algorithm) can improve the results even further.


## Code samples

* Serialization
```csharp
 byte[] avroObject = AvroConvert.Serialize(object yourObject);
```
<br/>

* Deserialization
```csharp
CustomClass deserializedObject = AvroConvert.Deserialize<CustomClass>(byte[] avroObject);
```
<br/>

* Read schema from Avro object

```csharp
string schemaInJsonFormat = AvroConvert.GetSchema(byte[] avroObject)
```
<br/>

* Deserialization of large collection of Avro objects one by one

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

* Generation of C# models from Avro file or schema

```csharp
  string resultModel = AvroConvert.GenerateModel(avroObject);
```

* Conversion of Avro to JSON directly

```csharp
  var resultJson = AvroConvert.Avro2Json(avroObject);
```


[Full Documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md)



## Related Work  

1) [AvroConvertOnline](https://adrianstrugala.github.io/AvroConvert/) - online Avro Schema to C# model converter

2) [![Nuget](https://img.shields.io/badge/Soltechnology.Avro.Http-v3.0.0-blue?logo=nuget)](https://www.nuget.org/packages/SolTechnology.Avro.Http/) - Library containing functionalities, which enable communication between microservices via Http using Avro data format

3) [![Nuget](https://img.shields.io/badge/Soltechnology.Avro.Kafka-v3.0.0-blue?logo=nuget)](https://www.nuget.org/packages/SolTechnology.Avro.Kafka/) - Library containing components needed for Confluent Kafka integration



## License  

The project is [CC BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) licensed.
\
For commercial purposes purchase AvroConvert on website - [Xabe.net](https://xabe.net/product/avroconvert/)


## Contribution

We want to improve AvroConvert as much as possible. If you have any idea, found next possible feature, optimization opportunity or better way for integration, leave a comment or pull request.


