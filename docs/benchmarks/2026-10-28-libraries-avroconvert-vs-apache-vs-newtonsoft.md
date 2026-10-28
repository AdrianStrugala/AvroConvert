# AvroConvert 4.0 vs Apache.Avro vs Newtonsoft.Json

Same POCO dataset (`User` with nested `Contact` and two `Offering`s) serialized to / deserialized from a `byte[]` by each library. Source: `benchmarks/LibrariesBenchmark`, chart: `libraries.png`, re-render with `scripts/plot-libraries.py`.

How each library is driven:

| Library | Version | Write | Read |
|---|---|---|---|
| AvroConvert | 4.0.0-preview.3 | `AvroConvert.Serialize(List<User>)` – Avro container file, one entry per record, codec `null` | `AvroConvert.Deserialize<List<User>>` |
| Apache.Avro | 1.12.2 | `Avro.Reflect.ReflectWriter<User>` + `DataFileWriter<User>` – Avro container file, one entry per record, codec `null` | `DataFileReader<User>` + `ReflectReader<User>` |
| Newtonsoft.Json | 13.0.4 | `JsonSerializer` → `JsonTextWriter` → UTF-8 bytes | `JsonTextReader` → `JsonSerializer.Deserialize<List<User>>` |

The two Avro outputs are byte-identical in size and cross-readable (checked by `dotnet run -- smoke`).

```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.7.1 (25G241) [Darwin 25.6.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  Job-WNYEYB : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a

Runtime=.NET 10.0  IterationCount=10  WarmupCount=3  

```
| Method                     | Records | Mean          | Error         | StdDev        | Gen0      | Payload     | Gen1      | Gen2     | Allocated   |
|--------------------------- |-------- |--------------:|--------------:|--------------:|----------:|------------:|----------:|---------:|------------:|
| **AvroConvert_Serialize**      | **1**       |      **1.081 μs** |     **0.0193 μs** |     **0.0115 μs** |    **0.4177** |       **969 B** |         **-** |        **-** |     **2.57 KB** |
| AvroConvert_Deserialize    | 1       |      1.234 μs |     0.0522 μs |     0.0311 μs |    0.6752 |       969 B |    0.0019 |        - |     4.14 KB |
| ApacheAvro_Serialize       | 1       |      4.880 μs |     0.1641 μs |     0.0976 μs |    2.2888 |       969 B |    0.0381 |        - |    14.06 KB |
| ApacheAvro_Deserialize     | 1       |     20.091 μs |     1.4928 μs |     0.9874 μs |    7.3242 |       969 B |    0.6409 |        - |    44.88 KB |
| NewtonsoftJson_Serialize   | 1       |      2.330 μs |     0.1396 μs |     0.0730 μs |    3.5896 |       370 B |    0.1793 |        - |    22.04 KB |
| NewtonsoftJson_Deserialize | 1       |      2.473 μs |     0.1609 μs |     0.1064 μs |    1.0567 |       370 B |    0.0114 |        - |     6.48 KB |
| **AvroConvert_Serialize**      | **1000**    |    **136.410 μs** |     **6.0547 μs** |     **4.0048 μs** |   **35.8887** |   **134,373 B** |   **35.6445** |  **35.6445** |   **133.07 KB** |
| AvroConvert_Deserialize    | 1000    |    240.723 μs |     8.8364 μs |     5.8447 μs |  135.0098 |   134,373 B |   53.9551 |        - |   828.78 KB |
| ApacheAvro_Serialize       | 1000    |  1,751.469 μs |    85.2196 μs |    56.3675 μs |  564.4531 |   134,373 B |  279.2969 | 183.5938 |  2979.89 KB |
| ApacheAvro_Deserialize     | 1000    |  2,797.656 μs |   157.4996 μs |   104.1762 μs |  332.0313 |   134,373 B |  164.0625 |        - |  2058.62 KB |
| NewtonsoftJson_Serialize   | 1000    |  1,449.582 μs |    14.6022 μs |     7.6372 μs |  392.5781 |   387,869 B |  328.1250 | 320.3125 |  1717.31 KB |
| NewtonsoftJson_Deserialize | 1000    |  2,168.526 μs |    62.5286 μs |    32.7037 μs |  144.5313 |   387,869 B |   58.5938 |        - |   902.57 KB |
| **AvroConvert_Serialize**      | **20000**   |  **2,463.188 μs** |    **51.9376 μs** |    **30.9072 μs** |  **101.5625** | **2,828,288 B** |  **101.5625** | **101.5625** |  **2764.27 KB** |
| AvroConvert_Deserialize    | 20000   | 19,368.806 μs |   381.7646 μs |   227.1820 μs | 3250.0000 | 2,828,288 B | 1468.7500 | 562.5000 | 16950.75 KB |
| ApacheAvro_Serialize       | 20000   | 29,517.164 μs |   678.1273 μs |   448.5392 μs | 7250.0000 | 2,828,288 B |  687.5000 | 406.2500 | 52843.48 KB |
| ApacheAvro_Deserialize     | 20000   | 74,590.006 μs | 1,010.3875 μs |   601.2654 μs | 6666.6667 | 2,828,288 B | 2333.3333 | 666.6667 |  38514.5 KB |
| NewtonsoftJson_Serialize   | 20000   | 27,507.047 μs |   581.7606 μs |   346.1964 μs | 1656.2500 | 8,031,570 B |  750.0000 | 687.5000 | 30183.08 KB |
| NewtonsoftJson_Deserialize | 20000   | 61,874.543 μs | 2,477.0279 μs | 1,638.4006 μs | 3222.2222 | 8,031,570 B | 1333.3333 | 444.4444 |  18250.7 KB |

Payload: uncompressed. JSON of a single record is smaller because the Avro container header carries the schema (~600 B); from a few dozen records up, Avro is ~2.9× smaller and compression codecs (Deflate, Snappy, GZip, Brotli) shrink it further.
