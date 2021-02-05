
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
  <a href="https://www.nuget.org/packages/AvroConvert"><img src="https://img.shields.io/badge/Nuget-v2.7.1-blue?logo=nuget"></a>
  <a href="https://github.com/AdrianStrugala/AvroConvert"><img src="https://img.shields.io/badge/Downloads-21k-blue?logo=github"></a>
  <a href="https://ci.appveyor.com/project/AdrianStrugala/avroconvert"><img src="https://img.shields.io/appveyor/build/AdrianStrugala/AvroConvert?logo=azuredevops"></a>
</p>


## Docs

**Avro format combines readability of JSON and compression of binary data serialization.**

[Introduction article](https://www.c-sharpcorner.com/blogs/avro-rest-api-as-the-evolution-of-json-based-communication-between-mic)

[Apache Wiki](https://cwiki.apache.org/confluence/display/AVRO/Index)

[General information](http://avro.apache.org/)

[Documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md)

## Benefits

The main purpose of the project was to enhance HTTP communication between microservices. Replacing JSON with Avro brought three main benefits:
* Decreased the communication time between microservices
* Reduced the network traffic by about 30%
* Increased communication security - the data was not visible in plain JSON text


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
| Data is encrypted                                       |                      ✔️                     |      ✔️      |        ❌        |

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
Using Avro for communication between your services significantly reduces communication time and network traffic. Additionally choosing encoding (compression algorithm) can improve the results even further.


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


[Full Documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md)


## Related packages  

[![Nuget](https://img.shields.io/badge/Soltechnology.Avro.Http-v2.7.0-blue?logo=nuget)](https://www.nuget.org/packages/SolTechnology.Avro.Http/) - Library containing functionalities, which enable communication between microservices via Http using Avro data format

[![Nuget](https://img.shields.io/badge/Soltechnology.Avro.Kafka-v0.1.0-blue?logo=nuget)](https://www.nuget.org/packages/SolTechnology.Avro.Kafka/) - Library containing extensions for Confluent Kafka platform - used together with Kafka Consumer and Producer



## License  

The project is [CC BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) licensed.
\
For commercial purposes purchase AvroConvert on website - [Xabe.net](https://xabe.net/product/avroconvert/)


## Contribution

We want to improve AvroConvert as much as possible. If you have any idea, found next possible feature, optimization opportunity or better way for integration, leave a comment or pull request. 


