# AvroConvert 4.0 vs Apache.Avro vs Newtonsoft.Json

Same POCO dataset (`User` with nested `Contact` and two `Offering`s) serialized to / deserialized from a `byte[]` by each library. Source: `tests/LibrariesBenchmark`, chart: `libraries.png`, re-render with `scripts/plot-libraries.py`.

How each library is driven:

| Library | Version | Write | Read |
|---|---|---|---|
| AvroConvert | 4.0.0-preview.1 | `AvroConvert.Serialize(List<User>)` – Avro container file, one entry per record, codec `null` | `AvroConvert.Deserialize<List<User>>` |
| Apache.Avro | 1.12.2 | `Avro.Reflect.ReflectWriter<User>` + `DataFileWriter<User>` – Avro container file, one entry per record, codec `null` | `DataFileReader<User>` + `ReflectReader<User>` |
| Newtonsoft.Json | 13.0.4 | `JsonSerializer` → `JsonTextWriter` → UTF-8 bytes | `JsonTextReader` → `JsonSerializer.Deserialize<List<User>>` |

The two Avro outputs are byte-identical in size and cross-readable (checked by `dotnet run -- smoke`).

```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.7 (25G229) [Darwin 25.6.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  Job-WNYEYB : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a

Runtime=.NET 10.0  IterationCount=10  WarmupCount=3  

```
| Method                     | Records | Mean          | Error         | StdDev      | Gen0      | Payload     | Gen1      | Gen2     | Allocated   |
|--------------------------- |-------- |--------------:|--------------:|------------:|----------:|------------:|----------:|---------:|------------:|
| AvroConvert_Serialize      | 1       |      1.455 μs |     0.1524 μs |   0.0907 μs |    0.9193 |       969 B |    0.0095 |        - |     5.64 KB |
| AvroConvert_Deserialize    | 1       |      1.072 μs |     0.0957 μs |   0.0633 μs |    0.6542 |       969 B |    0.0019 |        - |     4.01 KB |
| ApacheAvro_Serialize       | 1       |      4.993 μs |     0.2391 μs |   0.1581 μs |    2.2888 |       969 B |    0.0381 |        - |    14.06 KB |
| ApacheAvro_Deserialize     | 1       |     19.412 μs |     0.4249 μs |   0.2811 μs |    7.3242 |       969 B |    0.6409 |        - |    44.88 KB |
| NewtonsoftJson_Serialize   | 1       |      2.434 μs |     0.1956 μs |   0.1294 μs |    3.5896 |       370 B |    0.1793 |        - |    22.04 KB |
| NewtonsoftJson_Deserialize | 1       |      2.555 μs |     0.1934 μs |   0.1279 μs |    1.0567 |       370 B |    0.0114 |        - |     6.48 KB |
| AvroConvert_Serialize      | 1000    |    272.090 μs |    12.9195 μs |   7.6882 μs |  157.7148 |   134,373 B |  157.7148 | 157.7148 |   707.19 KB |
| AvroConvert_Deserialize    | 1000    |    284.872 μs |     3.9631 μs |   2.0728 μs |  134.7656 |   134,373 B |   53.7109 |        - |   828.79 KB |
| ApacheAvro_Serialize       | 1000    |  1,712.219 μs |    43.7242 μs |  26.0196 μs |  398.4375 |   134,373 B |  199.2188 | 199.2188 |  2979.68 KB |
| ApacheAvro_Deserialize     | 1000    |  2,663.263 μs |    52.2039 μs |  31.0657 μs |  332.0313 |   134,373 B |  164.0625 |        - |  2058.62 KB |
| NewtonsoftJson_Serialize   | 1000    |  1,461.433 μs |    76.2921 μs |  50.4625 μs |  396.4844 |   387,869 B |  328.1250 | 326.1719 |  1717.35 KB |
| NewtonsoftJson_Deserialize | 1000    |  2,143.414 μs |    39.9475 μs |  23.7721 μs |  144.5313 |   387,869 B |   58.5938 |        - |   902.57 KB |
| AvroConvert_Serialize      | 20000   |  4,595.582 μs |   194.7680 μs | 128.8270 μs |  476.5625 | 2,828,288 B |  445.3125 | 445.3125 | 10937.54 KB |
| AvroConvert_Deserialize    | 20000   | 20,124.271 μs |   400.5736 μs | 209.5078 μs | 3250.0000 | 2,828,288 B | 1468.7500 | 562.5000 | 16950.83 KB |
| ApacheAvro_Serialize       | 20000   | 30,417.277 μs |   531.9609 μs | 351.8592 μs | 7375.0000 | 2,828,288 B |  750.0000 | 562.5000 | 52844.29 KB |
| ApacheAvro_Deserialize     | 20000   | 78,175.650 μs | 1,460.3610 μs | 869.0374 μs | 6666.6667 | 2,828,288 B | 2333.3333 | 666.6667 | 38515.33 KB |
| NewtonsoftJson_Serialize   | 20000   | 27,648.988 μs |   798.6375 μs | 417.7029 μs | 1812.5000 | 8,031,570 B |  906.2500 | 843.7500 | 30183.42 KB |
| NewtonsoftJson_Deserialize | 20000   | 61,933.887 μs | 1,119.9781 μs | 585.7702 μs | 3222.2222 | 8,031,570 B | 1333.3333 | 444.4444 |  18250.7 KB |

Payload: uncompressed. JSON of a single record is smaller because the Avro container header carries the schema (~600 B); from a few dozen records up, Avro is ~2.9× smaller and compression codecs (Deflate, Snappy, GZip, Brotli) shrink it further.
