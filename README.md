
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
 <a href="https://www.nuget.org/packages/AvroConvert"><img src="https://img.shields.io/badge/Nuget-v3.4.10-blue?logo=nuget"></a>
 <a href="https://adrianstrugala.github.io/AvroConvert/"><img src="https://img.shields.io/badge/Downloads-899k-blue?logo=github"></a>
 <a href="https://github.com/AdrianStrugala/AvroConvert/actions/workflows/build&test.yml"><img src="https://github.com/AdrianStrugala/AvroConvert/actions/workflows/build&test.yml/badge.svg"></a>

</p>

## Docs

**Avro format combines readability of JSON and data compression of binary serialization.**

[Apache Avro format documentation](http://avro.apache.org/)

[First steps with Avro in the .NET article](https://www.c-sharpcorner.com/article/how-to-work-with-avro-data-type-in-net-environment/)

[Basic AvroConvert documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md)

[Advanced AvroConvert documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md)


## Features

|                                                               | AvroConvert                                | Apache.Avro | Newtonsoft.Json |
|---------------------------------------------------------------|:------------------------------------------:|:-----------:|:---------------:|
| Rapid serialization                                            |                      ✔️                     |      ✔️      |        ✔️        |                       
| Low memory allocation                                         |                      ✔️                     |      ✔️      |        ✔️        |
| Readable schema of data structure                                      |                      ✔️                     |      ✔️      |        ✔️        |
| Support for C# native objects (Dictionary, List, DateTime...) |                      ✔️                     |      ❌      |        ✔️        |
| Built-in data encryption                                          |                      ✔️                     |      ✔️      |        ❌        |
| Support for compression codecs                                | Deflate<br/>  Snappy<br/> GZip<br/> Brotli |   Deflate   |        ❌        |


//TODO: może to powinno przekierowywać do docsów i być w samples?


**Expanded list:** <br>
1. Serialization of .NET object to Avro format 
   - [Standard](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/AvroConvert.Serialize.cs)
   - [Excluding header](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/AvroConvert.SerializeHeadless.cs)
2. Deserialization of Avro data to .NET object 
   - [Standard](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/AvroConvert.Deserialize.cs)
   - [Excluding header](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/AvroConvert.DeserializeHeadless.cs)
   - [Processing collection items one by one](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/AvroConvert.DeserializeByLine.cs)
3. Other Avro data related operations
   - [Merge Avro objects](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/AvroConvert.Merge.cs)
   - [JSON to Avro conversion](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/AvroConvert.Json2Avro.cs)
   - [Avro to JSON conversion](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/AvroConvert.Avro2Json.cs)
4. Schema related
   - [Get schema from Avro data](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/AvroConvert.GetSchema.cs)
   - [Generate schema from .NET object](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/AvroConvert.GenerateSchema.cs)
   - [Generate .NET model from Avro data or schema](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/AvroConvert.GenerateModel.cs)
   - [Generate Avro schema for JSON data](https://github.com/AdrianStrugala/AvroConvert/blob/refactore-readme/src/AvroConvert/SchemaConvert.GenerateFromJson.cs)



[Full Changelog](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/CHANGELOG.md)


## Benefits

Introducing Avro to the projects brings three main benefits:
* Reduction of data size and storage cost
* Decrease of the communication time and the network traffic between microservices
* Increased security - the data is not visible in plain text format


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


[Basic documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md)

[Advanced documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md)




## License  

The project is [CC BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) licensed.
\
For commercial purposes purchase AvroConvert on website - [Xabe.net](https://xabe.net/product/avroconvert/)


## Contribution

We want to improve AvroConvert as much as possible. If you have any idea, found next possible feature, optimization opportunity or better way for integration, leave a comment or pull request.


Thank you a million [to all the contributors](https://github.com/AdrianStrugala/AvroConvert/graphs/contributors) to the library, including those that raise issues, started conversations, and those who send pull requests. Thank you!

These amazing people have contributed to AvroConvert:

<a href="https://github.com/AdrianStrugala/AvroConvert/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=AdrianStrugala/AvroConvert" />
</a>



## Related Work  

- [AvroConvertOnline](https://adrianstrugala.github.io/AvroConvert/) - online Avro Schema to C# model converter

- [![Nuget](https://img.shields.io/badge/Soltechnology.Avro.Http-v3.0.0-blue?logo=nuget)](https://www.nuget.org/packages/SolTechnology.Avro.Http/) - Library containing functionalities, which enable communication between microservices via Http using Avro data format

- [![Nuget](https://img.shields.io/badge/Soltechnology.Avro.Kafka-v3.0.0-blue?logo=nuget)](https://www.nuget.org/packages/SolTechnology.Avro.Kafka/) - Library containing components needed for Confluent Kafka integration

- [Avro API article](https://www.c-sharpcorner.com/blogs/avro-rest-api-as-the-evolution-of-json-based-communication-between-mic) 
