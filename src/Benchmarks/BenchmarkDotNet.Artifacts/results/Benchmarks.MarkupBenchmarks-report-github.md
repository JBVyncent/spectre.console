```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6937/22H2/2022Update)
Intel Core i7-6700 CPU 3.40GHz (Max: 3.41GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.103
  [Host]    : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3

Job=.NET 10.0  Runtime=.NET 10.0  

```
| Method             | Mean     | Error     | StdDev    | Gen0   | Allocated |
|------------------- |---------:|----------:|----------:|-------:|----------:|
| Markup_Constructor | 1.824 μs | 0.0250 μs | 0.0316 μs | 0.7343 |      3 KB |
| AnsiMarkup_Parse   | 1.243 μs | 0.0235 μs | 0.0242 μs | 0.5245 |   2.15 KB |
