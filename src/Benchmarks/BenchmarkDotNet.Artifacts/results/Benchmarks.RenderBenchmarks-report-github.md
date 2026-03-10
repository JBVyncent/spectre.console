```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6937/22H2/2022Update)
Intel Core i7-6700 CPU 3.40GHz (Max: 3.41GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.103
  [Host]    : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.24 (8.0.24, 8.0.2426.7010), X64 RyuJIT x86-64-v3


```
| Method | Job       | Runtime   | Mean     | Error    | StdDev   | Median   | Gen0   | Gen1   | Allocated |
|------- |---------- |---------- |---------:|---------:|---------:|---------:|-------:|-------:|----------:|
| Render | .NET 10.0 | .NET 10.0 | 44.45 μs | 1.957 μs | 5.771 μs | 42.74 μs | 0.7935 | 0.0610 |    3.5 KB |
| Render | .NET 8.0  | .NET 8.0  | 62.79 μs | 1.249 μs | 3.268 μs | 61.76 μs | 0.8545 | 0.1221 |   3.97 KB |
| Render | .NET 9.0  | .NET 9.0  |       NA |       NA |       NA |       NA |     NA |     NA |        NA |

Benchmarks with issues:
  RenderBenchmarks.Render: .NET 9.0(Runtime=.NET 9.0)
