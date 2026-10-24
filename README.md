<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="./docs/logo-dark.png">
    <img alt="SolTechnology / AvroConvert" src="./docs/logo.png" width="300">
  </picture>
</p>

<p align="center">
  Rapid Apache Avro serializer for .NET
</p>

<p align="center">
 <a href="https://github.com/AdrianStrugala/AvroConvert"><img src="https://img.shields.io/github/stars/AdrianStrugala/AvroConvert.svg?label=%E2%98%85%20Stars%20%E2%98%85&color=yellow&style=flat&logo=github"></a>
 <a href="https://adrianstrugala.github.io/AvroConvert/"><img src="https://img.shields.io/badge/AvroConvertOnline-Try%20Now-4C6A8A.svg?logo=google-chrome"></a>
 <br>
 <a href="https://github.com/AdrianStrugala/AvroConvert/actions/workflows/build&test.yml"><img src="https://img.shields.io/github/actions/workflow/status/AdrianStrugala/AvroConvert/build%26test.yml?label=Build%26Test&logo=github"></a>
 <a href="./LICENSE.md"><img src="https://img.shields.io/badge/licence-free%20under%20%241M%20%C2%B7%20commercial-2F7D7A.svg?logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAxNiAxNiIgZmlsbD0iI2ZmZiI%2BPHBhdGggZD0iTTguNzUuNzVWMmguOTg1Yy4zMDQgMCAuNjAzLjA4Ljg2Ny4yMzFsMS4yOS43MzZjLjAzOC4wMjIuMDguMDMzLjEyNC4wMzNoMi4yMzRhLjc1Ljc1IDAgMCAxIDAgMS41aC0uNDI3bDIuMTExIDQuNjkyYS43NS43NSAwIDAgMS0uMTU0LjgzOGwtLjUzLS41My41MjkuNTMxLS4wMDEuMDAyLS4wMDIuMDAyLS4wMDYuMDA2LS4wMDYuMDA1LS4wMS4wMS0uMDQ1LjA0Yy0uMjEuMTc2LS40NDEuMzI3LS42ODYuNDVDMTQuNTU2IDEwLjc4IDEzLjg4IDExIDEzIDExYTQuNDk4IDQuNDk4IDAgMCAxLTIuMDIzLS40NTQgMy41NDQgMy41NDQgMCAwIDEtLjY4Ni0uNDVsLS4wNDUtLjA0LS4wMTYtLjAxNS0uMDA2LS4wMDYtLjAwNC0uMDA0di0uMDAxYS43NS43NSAwIDAgMS0uMTU0LS44MzhMMTIuMTc4IDQuNWgtLjE2MmMtLjMwNSAwLS42MDQtLjA3OS0uODY4LS4yMzFsLTEuMjktLjczNmEuMjQ1LjI0NSAwIDAgMC0uMTI0LS4wMzNIOC43NVYxM2gyLjVhLjc1Ljc1IDAgMCAxIDAgMS41aC02LjVhLjc1Ljc1IDAgMCAxIDAtMS41aDIuNVYzLjVoLS45ODRhLjI0NS4yNDUgMCAwIDAtLjEyNC4wMzNsLTEuMjg5LjczN2MtLjI2NS4xNS0uNTY0LjIzLS44NjkuMjNoLS4xNjJsMi4xMTIgNC42OTJhLjc1Ljc1IDAgMCAxLS4xNTQuODM4bC0uNTMtLjUzLjUyOS41MzEtLjAwMS4wMDItLjAwMi4wMDItLjAwNi4wMDYtLjAxNi4wMTUtLjA0NS4wNGMtLjIxLjE3Ni0uNDQxLjMyNy0uNjg2LjQ1QzQuNTU2IDEwLjc4IDMuODggMTEgMyAxMWE0LjQ5OCA0LjQ5OCAwIDAgMS0yLjAyMy0uNDU0IDMuNTQ0IDMuNTQ0IDAgMCAxLS42ODYtLjQ1bC0uMDQ1LS4wNC0uMDE2LS4wMTUtLjAwNi0uMDA2LS4wMDQtLjAwNHYtLjAwMWEuNzUuNzUgMCAwIDEtLjE1NC0uODM4TDIuMTc4IDQuNUgxLjc1YS43NS43NSAwIDAgMSAwLTEuNWgyLjIzNGEuMjQ5LjI0OSAwIDAgMCAuMTI1LS4wMzNsMS4yODgtLjczN2MuMjY1LS4xNS41NjQtLjIzLjg2OS0uMjNoLjk4NFYuNzVhLjc1Ljc1IDAgMCAxIDEuNSAwWm0yLjk0NSA4LjQ3N2MuMjg1LjEzNS43MTguMjczIDEuMzA1LjI3M3MxLjAyLS4xMzggMS4zMDUtLjI3M0wxMyA2LjMyN1ptLTEwIDBjLjI4NS4xMzUuNzE4LjI3MyAxLjMwNS4yNzNzMS4wMi0uMTM4IDEuMzA1LS4yNzNMMyA2LjMyN1oiLz48L3N2Zz4%3D"></a>
 <a href="https://www.nuget.org/packages/AvroConvert"><img src="https://img.shields.io/nuget/dt/AvroConvert.svg?label=NuGet%20Downloads&logo=nuget&color=blue"></a>
</p>

## Docs

[AvroConvert documentation](https://github.com/AdrianStrugala/AvroConvert/tree/master/docs)

[Apache Avro specification](http://avro.apache.org/)

[First steps with Avro in .NET](https://www.c-sharpcorner.com/article/how-to-work-with-avro-data-type-in-net-environment/)


## Why AvroConvert

**Avro combines the self-describing schema of JSON with the size and speed of binary serialization.** AvroConvert brings it to .NET the way you already serialize JSON: plain C# objects in, `byte[]` out – no code generation, no `.avsc` files.

|                                                                          |                 AvroConvert                 |               Apache.Avro                | Newtonsoft.Json |
|--------------------------------------------------------------------------|:-------------------------------------------:|:----------------------------------------:|:---------------:|
| Works on plain C# classes – no generated code or schema files            |                     ✔️                      |                    ❌                     |       ✔️        |
| Native .NET types (`Dictionary`, `List`, `DateTime`, `Guid`, `decimal`…) |                     ✔️                      |                    ❌                     |       ✔️        |
| Compact binary payload                                                   |                     ✔️                      |                    ✔️                     |       ❌        |
| Schema stored with the data – readable, versionable, evolvable           |                     ✔️                      |                    ✔️                     |       ❌        |
| Compression codecs                                                       | Deflate · Snappy · Zstandard · GZip · Brotli (built-in) |      Deflate (others via add-ons)       |       ❌        |
| Serialization time                                                       |                     1×                      |                 6× slower                 |    5× slower    |
| Deserialization time                                                     |                     1×                      |                 9× slower                 |    3× slower    |
| Memory allocated                                                         |                     1×                      |                 3× more                   |     2× more     |
| Payload size, uncompressed                                               |                     1×                      |                    1×                     |    2.9× larger  |

What it means for your services:
* **Smaller data** – a 1 000-record payload is 2.9× smaller than JSON before compression, and codecs shrink it further → lower storage and network cost
* **Faster round trips** – less time spent in serialization and on the wire between microservices
* **Safer by default** – binary on the wire is not readable in plain text, and the embedded schema protects against silent shape drift

Benchmarks: [raw results](docs/benchmarks/2026-10-08-libraries-avroconvert-vs-apache-vs-newtonsoft.md) · `benchmarks/LibrariesBenchmark`


## Features


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

* Streaming of large collections, item by item

```csharp
AvroConvert.Serialize(items, fileStream);

await foreach (var item in AvroConvert.Deserialize<CustomClass>(stream))
{
    // process item
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

**Extended list:** <br>
1. Serialization of .NET object to Avro format
   - [Standard, codecs, collections](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#serialization)
   - [Excluding header](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#headless-serialization-and-deserialization)
   - [Streaming](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#streaming)
2. Deserialization of Avro data to .NET object
   - [Standard, dynamic, unions](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#deserialization)
   - [Excluding header](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#headless-serialization-and-deserialization)
   - [Schema evolution: reading data written with another schema version](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#schema-evolution)
   - [Processing collection items one by one](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#reading-items-one-by-one)
3. Other Avro data related operations
   - [Merge Avro objects](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#merge)
   - [JSON to Avro conversion](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#json2avro)
   - [Avro to JSON conversion](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#avro2json)
   - [Avro JSON Encoding (spec format, avro-tools compatible)](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#avro-json-encoding)
4. Customization
   - [Options reference](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#options-reference)
   - [Custom converters](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#custom-converters)
   - [Naming policy](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.md#naming-policy)
5. Schema related
   - [Get schema from Avro data](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.Schema.md#reading-the-schema-of-avro-data)
   - [Generate schema from .NET type: type mapping and attributes](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.Schema.md#generating-a-schema-from-c-types)
   - [Generate .NET model from Avro data or schema](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.Schema.md#generate-model)
   - [Generate Avro schema for JSON data](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.Schema.md#avroschema)
   - [Logical types](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.Schema.md#logical-types)
   - [AvroSchema: canonical form, fingerprints, comparison and compatibility checks](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.Schema.md#avroschema)


[Full documentation](https://github.com/AdrianStrugala/AvroConvert/tree/master/docs)


## License

AvroConvert is source-available. Noncommercial use and small companies pay nothing; everyone else buys a yearly licence for the whole organisation.

| You are… | Licence | Cost |
|---|---|---|
| Individual, student, non-profit, public institution, research or evaluation | [PolyForm Noncommercial 1.0.0](licenses/PolyForm-Noncommercial-1.0.0.md) | Free |
| Company with fewer than 100 people **and** under 1,000,000 USD annual revenue | [PolyForm Small Business 1.0.0](licenses/PolyForm-Small-Business-1.0.0.md) | Free |
| Any other company or organisation | [Commercial Licence](licenses/Commercial.md) — Business under 1,000 people, Enterprise from 1,000 people, listed companies and public sector | Annual, per organisation, unlimited developers — [buy at soltechnology.dev](https://soltechnology.dev/avroconvert/) |

Say which licence you rely on, in code or through the `AVROCONVERT_LICENSE` environment variable (`NonCommercial`, `SmallBusiness` or the key). It is optional for the free licences and expected for commercial ones; nothing is enforced and nothing phones home.

```csharp
AvroConvert.License = AvroLicense.NonCommercial;          // or AvroLicense.SmallBusiness
AvroConvert.License = AvroLicense.Commercial("AVC1.…");   // licence key received by e-mail after purchase
```

Details in [LICENSE.md](LICENSE.md).

Versions 3.x stay under CC BY-NC-SA 3.0. A commercial licence bought for 4.x also covers the 3.4.x releases, so a team that is not on .NET 10 yet can be licensed now and upgrade later. If you bought a 3.x licence from Xabe, it still covers 3.x, and you can have a free perpetual licence for 4.x and every later version: send the invoice to contact@soltechnology.dev, there is no deadline ([licenses/Commercial.md](licenses/Commercial.md), section 12).

## Contribution

We want to improve AvroConvert as much as possible. If you have any idea, found next possible feature, optimization opportunity or better way for integration, leave a comment or pull request.


Thank you a million [to all the contributors](https://github.com/AdrianStrugala/AvroConvert/graphs/contributors) to the library, including those that raise issues, started conversations, and those who send pull requests. Thank you!

These amazing people have contributed to AvroConvert:

<a href="https://github.com/AdrianStrugala/AvroConvert/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=AdrianStrugala/AvroConvert" />
</a>


## Related Work  

- [![](https://img.shields.io/badge/AvroConvertOnline-Try%20Now-blue.svg?logo=google-chrome)](https://adrianstrugala.github.io/AvroConvert/) – paste an Avro schema, get a C# model
- [![Nuget](https://img.shields.io/badge/Soltechnology.Avro.Http-v3.0.0-blue?logo=nuget)](https://www.nuget.org/packages/SolTechnology.Avro.Http/) – Avro as the wire format between HTTP services
- [![Nuget](https://img.shields.io/badge/Soltechnology.Avro.Kafka-v3.0.0-blue?logo=nuget)](https://www.nuget.org/packages/SolTechnology.Avro.Kafka/) – serializers for Confluent Kafka
- [Avro API article](https://www.c-sharpcorner.com/blogs/avro-rest-api-as-the-evolution-of-json-based-communication-between-mic) 

---

Apache Avro, Avro and Apache are trademarks of [The Apache Software Foundation](https://www.apache.org/). AvroConvert is an independent implementation of the Avro specification for .NET and is not affiliated with, endorsed by or sponsored by the ASF.
