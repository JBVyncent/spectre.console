```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6937/22H2/2022Update)
Intel Core i7-6700 CPU 3.40GHz (Max: 3.41GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.103
  [Host]    : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3

Job=.NET 10.0  Runtime=.NET 10.0  

```
| Method | text                 | Mean     | Error    | StdDev   | Gen0   | Allocated |
|------- |--------------------- |---------:|---------:|---------:|-------:|----------:|
| **Append** | **This (...)ines. [31]** | **465.7 ns** |  **9.34 ns** | **18.87 ns** | **0.2332** |     **976 B** |
| **Append** | **This (...)line. [31]** | **755.0 ns** | **14.99 ns** | **35.63 ns** | **0.3633** |    **1520 B** |
