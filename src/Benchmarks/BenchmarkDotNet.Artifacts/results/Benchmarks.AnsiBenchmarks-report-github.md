```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6937/22H2/2022Update)
Intel Core i7-6700 CPU 3.40GHz (Max: 3.41GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.103
  [Host]    : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.24 (8.0.24, 8.0.2426.7010), X64 RyuJIT x86-64-v3


```
| Method           | Job       | Runtime   | Mean     | Error    | StdDev   | Gen0   | Allocated |
|----------------- |---------- |---------- |---------:|---------:|---------:|-------:|----------:|
| RenderableToAnsi | .NET 10.0 | .NET 10.0 | 38.18 μs | 0.759 μs | 0.960 μs | 0.9155 |   3.92 KB |
| RenderableToAnsi | .NET 8.0  | .NET 8.0  | 60.22 μs | 1.123 μs | 1.813 μs | 0.9766 |   4.38 KB |
| RenderableToAnsi | .NET 9.0  | .NET 9.0  |       NA |       NA |       NA |     NA |        NA |

Benchmarks with issues:
  AnsiBenchmarks.RenderableToAnsi: .NET 9.0(Runtime=.NET 9.0)
