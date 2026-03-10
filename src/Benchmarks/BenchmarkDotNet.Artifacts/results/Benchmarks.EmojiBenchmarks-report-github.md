```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6937/22H2/2022Update)
Intel Core i7-6700 CPU 3.40GHz (Max: 3.41GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.103
  [Host]    : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v3

Job=.NET 10.0  Runtime=.NET 10.0  

```
| Method                   | Mean       | Error     | StdDev    | Gen0   | Allocated |
|------------------------- |-----------:|----------:|----------:|-------:|----------:|
| Replace_NoEmoji          |   4.085 ns | 0.1153 ns | 0.1079 ns |      - |         - |
| Replace_NoEmoji_ButColon |   9.438 ns | 0.1067 ns | 0.0891 ns |      - |         - |
| Replace_Emoji            | 116.025 ns | 3.4124 ns | 9.7909 ns | 0.0707 |     296 B |
