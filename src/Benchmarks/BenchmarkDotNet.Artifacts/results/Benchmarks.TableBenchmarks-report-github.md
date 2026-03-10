```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6937/22H2/2022Update)
Intel Core i7-6700 CPU 3.40GHz (Max: 3.41GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.103
  [Host]    : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3

Job=.NET 10.0  Runtime=.NET 10.0  

```
| Method                  | Mean        | Error      | StdDev     | Median      | Gen0     | Gen1     | Gen2    | Allocated  |
|------------------------ |------------:|-----------:|-----------:|------------:|---------:|---------:|--------:|-----------:|
| SmallTable_3x3          |    66.67 μs |   1.289 μs |   1.076 μs |    66.52 μs |   9.7656 |        - |       - |   40.73 KB |
| MediumTable_5x20        |   248.20 μs |   5.761 μs |  16.620 μs |   247.03 μs |  71.7773 |   0.2441 |       - |  293.62 KB |
| LargeTable_10x100       | 3,432.22 μs | 111.782 μs | 317.107 μs | 3,334.19 μs | 437.5000 | 128.9063 | 62.5000 | 2774.16 KB |
| StyledTable_WithBorders |    96.10 μs |   2.020 μs |   5.285 μs |    94.89 μs |  16.8457 |        - |       - |   69.41 KB |
