# Phase 0 baseline – release/4.0 @ 451aa3b (net10.0, before phase-1 optimisations)

```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.7 (25G229) [Darwin 25.6.0]
Apple M1 Max, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  Job-WNYEYB : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a

Runtime=.NET 10.0  IterationCount=10  WarmupCount=3  

```
| Method                            | Mean          | Error         | StdDev        | Gen0       | Gen1      | Gen2      | Allocated   |
|---------------------------------- |--------------:|--------------:|--------------:|-----------:|----------:|----------:|------------:|
| Serialize_Flat                    |      2.667 μs |     0.0891 μs |     0.0589 μs |     1.3733 |    0.0191 |         - |     8.42 KB |
| Deserialize_Flat                  |     16.941 μs |     0.6113 μs |     0.3638 μs |     5.2185 |    0.2136 |         - |    31.98 KB |
| Serialize_FlatList1k              |    247.702 μs |     9.1613 μs |     5.4517 μs |   101.0742 |   31.2500 |   16.6016 |   628.22 KB |
| Deserialize_FlatList1k            |    439.417 μs |     8.7230 μs |     5.1909 μs |    87.8906 |   25.3906 |         - |   547.48 KB |
| Serialize_IntList10k              |    182.559 μs |     6.9731 μs |     4.1496 μs |    75.4395 |   18.7988 |         - |   464.16 KB |
| Deserialize_IntList10k            |    151.789 μs |     7.1453 μs |     4.2521 μs |    53.2227 |   10.4980 |         - |   326.71 KB |
| Serialize_Dictionary500           |    111.974 μs |     4.1623 μs |     2.7531 μs |    54.4434 |   12.0850 |         - |   335.13 KB |
| Deserialize_Dictionary500         |    208.322 μs |     3.9063 μs |     2.3246 μs |    56.8848 |   12.9395 |         - |   349.49 KB |
| Serialize_Nested3                 |      2.705 μs |     0.1434 μs |     0.0853 μs |     1.4610 |    0.0191 |         - |     8.97 KB |
| Deserialize_Nested3               |     15.178 μs |     0.1983 μs |     0.1037 μs |     5.2795 |    0.2136 |         - |     32.4 KB |
| Serialize_MultiBlock20k           | 11,032.640 μs |   119.0069 μs |    78.7157 μs |  2031.2500 | 1031.2500 | 1000.0000 | 18061.57 KB |
| Deserialize_MultiBlock20k         | 57,547.117 μs | 1,769.3116 μs | 1,170.2901 μs | 13500.0000 | 4000.0000 | 1000.0000 | 80867.77 KB |
| Serialize_MultiBlock20k_Deflate   | 33,867.892 μs | 1,147.6115 μs |   682.9252 μs |  2062.5000 | 1062.5000 | 1062.5000 | 17617.49 KB |
| Deserialize_MultiBlock20k_Deflate | 59,063.424 μs | 2,659.9924 μs | 1,582.9188 μs | 13500.0000 | 3500.0000 | 1000.0000 | 85719.61 KB |
