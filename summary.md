``` ini
BenchmarkDotNet=v0.13.2, OS=Windows 11 (10.0.22000.856/21H2)
Intel Core i7-9750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.401
  [Host]     : .NET 5.0.17 (5.0.1722.21314), X64 RyuJIT AVX2
  DefaultJob : .NET 5.0.17 (5.0.1722.21314), X64 RyuJIT AVX2
```

### Get cached + deserialization only for comparison

|                                                  Method |     Mean |     Error |    StdDev |    Median |     Gen0 |     Gen1 |    Gen2 | Allocated |
|-------------------------------------------------------- |---------:|----------:|----------:|----------:|---------:|---------:|--------:|----------:|
| GetCachedItemsPage (static JsonSerializerOptions)       | 6.246 ms | 0.1196 ms | 0.1119 ms |  6.285 ms | 277.7778 | 122.2222 | 22.2222 |   1.68 MB |
| GetCachedItemsPage (recalculated JsonSerializerOptions) | 10.44 ms | 0.200  ms | 0.238  ms | 10.38  ms | 300.0000 | 140.0000 | 20.0000 |   1.88 MB |
| DeserializeItemsPage                                    | 1.446 ms | 0.0236 ms | 0.0220 ms |  1.442 ms |  47.5000 |  15.0000 |         | 291.44 KB |

### SnakeCaseNamingPolicy.ConvertName

|                                Method |       Mean |    Error |   StdDev |     Median |   Gen0 | Allocated |
|-------------------------------------- |-----------:|---------:|---------:|-----------:|-------:|----------:|
|   ConvertConventionalPropertyNameSlow | 1,565.9 ns | 30.67 ns | 43.00 ns | 1,571.3 ns | 0.3060 |    1920 B |
|   ConvertConventionalPropertyNameFast |   364.1 ns |  6.74 ns |  7.21 ns |   365.3 ns | 0.0760 |     480 B |
| ConvertUnconventionalPropertyNameSlow | 2,672.2 ns | 50.24 ns | 46.99 ns | 2,659.9 ns | 0.4820 |    3024 B |
| ConvertUnconventionalPropertyNameFast |   439.2 ns |  8.62 ns | 11.20 ns |   438.8 ns | 0.1120 |     704 B |

Conventional property name used: `"HypotheticalPropertyNameWithUnrealisticlyManyWords"`

Unconventional property name used: `"HyPOThETicAl_PRopeRtYnAmE_wiThUNReAlisTicLyMaNy_wOrDs"`
