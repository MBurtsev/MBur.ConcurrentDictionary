Lock-Free hash table is presented to your attention. The implementation is much better than the .Net Core version. The API is fully compatible with the current version 3.0. The hash table is presented in two versions A and B. In the repository, you can find unit tests proving the absence of bugs. Benchmarks of performance and memory usage are also presented. For performance tests used ![BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet), also a mechanism was applied for simultaneously starting threads as much as possible. Below is a list of advantages and disadvantages. [See for more information](https://www.linkedin.com/pulse/lock-free-hash-table-maksim-burtsev/)

**ATTENTION: A flaw has been found in these implementations. Use only if you are sure that key.GetHashCode returns a unique code. For example, there is no guarantee for the string type.**

## ConcurrentDictionary_A

### Advantage

* TryAdd faster from 400% to 450%
* TryRemove faster from 60% to 420%
* TryUpdate faster from 60% to 250%
* GetOrUpdate faster from 400% to 480%
* AddOrUpdate faster from 400% to 650%
* ContainsKey faster from 15% to 60%
* Functions GetValues, GetKeys, CopyTo and others do not block the hash-table.
* Functions with callbacks like "TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)" only calls the factory if the value is actually used. The current version can call the factory even if the received value will not used.
* During grow, the hash-table is not locked.
* Memory usage overage three times less.

### Disadvantage

* TryGet slower with 1-4 threads maximum of 20%, after faster by 10%. But in the test, threads are accessed by different keys. If threads access the same key, the result will be worse.

## ConcurrentDictionary_B

### Advantage

* TryAdd faster from 280% to 400%
* TryRemove slower with 1-2 threads 17%-6%, after faster from 16% to 160%
* GetOrAdd faster from 270% to 500%
* AddOrUpdate faster from 380% to 600%
* TryGet faster from 1% to 30% (fully wait-free TryGet and Enumeration)
* Functions GetValues, GetKeys, CopyTo and others do not block the hash-table.
* Functions with callbacks like "TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)" only calls the factory if the value is actually used. The current version can call the factory even if the received value will not used.
* During grow, the hash-table is not locked.
* Memory usage overage 2,5 times less

### Disadvantage

* TryUpdate slower from 9% to 48%
* If the key or value is a reference type, then a certain number of objects will not be collected by heap. The internal implementation of the hash table uses a write buffer that stores the latest links. If the hash table is not used for a long time, these links may remain in the internal cycle buffer. The problem is not terrible if the objects do not take up a lot of memory, or if these objects are used in the application. Otherwise, it is recommended to use the Clear method if the hash table is no longer needed.

[The chart document can be viewed here](https://docs.google.com/spreadsheets/d/1QEh_A9CcXZr5duHLOoU2te1xmSas_2eU_dbvK5dA0Nk/edit?usp=sharing)

![1](https://i.imgur.com/09OU81J.png)

![2](https://i.imgur.com/gixyx4L.png)

![3](https://i.imgur.com/1qkqgC0.png)

![4](https://i.imgur.com/hWQCqTv.png)

![5](https://i.imgur.com/fjcanEs.png)

![6](https://i.imgur.com/yl9VIER.png)

![7](https://i.imgur.com/yoKubjS.png)

![8](https://i.imgur.com/ryvAe3y.png)


### Raw benchmark data for ConcurrentDictionary_A

``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 8.1 (6.3.9600.0)
Intel Xeon CPU E5-2640 v3 2.60GHz, 2 CPU, 32 logical and 16 physical cores
Frequency=2533198 Hz, Resolution=394.7579 ns, Timer=TSC
.NET Core SDK=3.0.100
  [Host]    : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  MediumRun : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT

Job=MediumRun  InvocationCount=1  IterationCount=5  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
|      Method | Threads |        Mean |        Error |     StdDev |         Min |         Max |         Op/s |     Op/s total |
|------------ |-------- |------------:|-------------:|-----------:|------------:|------------:|-------------:|--------------- |
|         **Add** |       **1** |    **84.03 ns** |    **13.443 ns** |   **3.491 ns** |    **81.85 ns** |    **90.22 ns** | **11,900,611.7** |  **11,900,611.70** |
| TryGetValue |       1 |    17.66 ns |     1.662 ns |   0.432 ns |    17.34 ns |    18.37 ns | 56,626,453.8 |  56,626,453.80 |
|      Remove |       1 |    27.77 ns |     3.651 ns |   0.948 ns |    27.06 ns |    29.40 ns | 36,004,764.3 |  36,004,764.30 |
|      Update |       1 |    23.79 ns |     2.175 ns |   0.565 ns |    23.26 ns |    24.58 ns | 42,036,923.9 |  42,036,923.90 |
|    GetOrAdd |       1 |    80.68 ns |     5.619 ns |   1.459 ns |    79.04 ns |    82.73 ns | 12,394,169.7 |  12,394,169.70 |
| AddOrUpdate |       1 |   105.56 ns |    10.476 ns |   2.721 ns |   102.63 ns |   109.22 ns |  9,473,325.0 |   9,473,325.00 |
|    Contains |       1 |    11.10 ns |     0.639 ns |   0.166 ns |    10.94 ns |    11.31 ns | 90,095,914.6 |  90,095,914.60 |
|         **Add** |       **2** |   **134.06 ns** |    **22.244 ns** |   **5.777 ns** |   **124.60 ns** |   **139.10 ns** |  **7,459,117.3** |  **14,918,234.60** |
| TryGetValue |       2 |    18.29 ns |     2.928 ns |   0.760 ns |    17.80 ns |    19.62 ns | 54,682,512.2 | 109,365,024.40 |
|      Remove |       2 |    29.28 ns |     3.086 ns |   0.801 ns |    28.27 ns |    30.44 ns | 34,157,922.7 |  68,315,845.40 |
|      Update |       2 |    24.97 ns |     5.617 ns |   1.459 ns |    24.11 ns |    27.55 ns | 40,043,495.9 |  80,086,991.80 |
|    GetOrAdd |       2 |   122.10 ns |     9.287 ns |   2.412 ns |   119.03 ns |   124.93 ns |  8,189,816.9 |  16,379,633.80 |
| AddOrUpdate |       2 |   127.41 ns |    14.784 ns |   3.839 ns |   123.23 ns |   131.83 ns |  7,848,386.4 |  15,696,772.80 |
|    Contains |       2 |    11.69 ns |     2.350 ns |   0.610 ns |    11.11 ns |    12.66 ns | 85,511,333.3 | 171,022,666.60 |
|         **Add** |       **3** |   **223.08 ns** |    **58.599 ns** |  **15.218 ns** |   **203.39 ns** |   **245.11 ns** |  **4,482,750.0** |  **13,448,250.00** |
| TryGetValue |       3 |    19.34 ns |     4.488 ns |   1.165 ns |    17.90 ns |    20.65 ns | 51,707,626.7 | 155,122,880.10 |
|      Remove |       3 |    29.39 ns |     1.167 ns |   0.303 ns |    29.13 ns |    29.91 ns | 34,020,384.9 | 102,061,154.70 |
|      Update |       3 |    25.38 ns |     1.914 ns |   0.497 ns |    24.69 ns |    25.81 ns | 39,396,633.2 | 118,189,899.60 |
|    GetOrAdd |       3 |   221.59 ns |    59.642 ns |  15.489 ns |   204.00 ns |   244.90 ns |  4,512,811.9 |  13,538,435.70 |
| AddOrUpdate |       3 |   208.06 ns |    28.301 ns |   7.350 ns |   196.97 ns |   215.40 ns |  4,806,344.4 |  14,419,033.20 |
|    Contains |       3 |    12.61 ns |     3.372 ns |   0.876 ns |    11.41 ns |    13.58 ns | 79,286,247.9 | 237,858,743.70 |
|         **Add** |       **4** |   **230.83 ns** |    **30.357 ns** |   **7.884 ns** |   **224.77 ns** |   **244.38 ns** |  **4,332,214.0** |  **17,328,856.00** |
| TryGetValue |       4 |    19.51 ns |     3.482 ns |   0.904 ns |    18.32 ns |    20.76 ns | 51,259,555.0 | 205,038,220.00 |
|      Remove |       4 |    32.84 ns |    10.121 ns |   2.628 ns |    29.92 ns |    35.96 ns | 30,451,908.0 | 121,807,632.00 |
|      Update |       4 |    27.14 ns |     7.990 ns |   2.075 ns |    25.25 ns |    30.31 ns | 36,843,879.7 | 147,375,518.80 |
|    GetOrAdd |       4 |   231.64 ns |    67.396 ns |  17.503 ns |   219.70 ns |   259.51 ns |  4,317,125.8 |  17,268,503.20 |
| AddOrUpdate |       4 |   220.05 ns |    25.105 ns |   6.520 ns |   212.75 ns |   229.57 ns |  4,544,349.3 |  18,177,397.20 |
|    Contains |       4 |    12.88 ns |     2.403 ns |   0.624 ns |    11.91 ns |    13.45 ns | 77,623,978.8 | 310,495,915.20 |
|         **Add** |       **5** |   **387.15 ns** |    **41.166 ns** |  **10.691 ns** |   **376.98 ns** |   **400.62 ns** |  **2,582,986.0** |  **12,914,930.00** |
| TryGetValue |       5 |    21.15 ns |     3.178 ns |   0.825 ns |    20.22 ns |    22.47 ns | 47,274,651.2 | 236,373,256.00 |
|      Remove |       5 |    37.90 ns |    11.417 ns |   2.965 ns |    35.22 ns |    42.28 ns | 26,383,054.5 | 131,915,272.50 |
|      Update |       5 |    30.92 ns |     9.128 ns |   2.371 ns |    28.06 ns |    33.17 ns | 32,340,899.8 | 161,704,499.00 |
|    GetOrAdd |       5 |   423.28 ns |    72.896 ns |  18.931 ns |   395.61 ns |   442.96 ns |  2,362,508.1 |  11,812,540.50 |
| AddOrUpdate |       5 |   452.01 ns |   223.233 ns |  57.973 ns |   396.42 ns |   550.18 ns |  2,212,357.2 |  11,061,786.00 |
|    Contains |       5 |    14.27 ns |     3.359 ns |   0.872 ns |    13.50 ns |    15.49 ns | 70,099,609.5 | 350,498,047.50 |
|         **Add** |       **6** |   **429.13 ns** |    **88.551 ns** |  **22.996 ns** |   **402.35 ns** |   **460.67 ns** |  **2,330,300.1** |  **13,981,800.60** |
| TryGetValue |       6 |    20.90 ns |     3.861 ns |   1.003 ns |    19.28 ns |    21.72 ns | 47,847,575.2 | 287,085,451.20 |
|      Remove |       6 |    33.39 ns |    15.789 ns |   4.100 ns |    29.78 ns |    40.30 ns | 29,948,286.8 | 179,689,720.80 |
|      Update |       6 |    28.04 ns |     2.840 ns |   0.738 ns |    27.37 ns |    29.09 ns | 35,659,327.9 | 213,955,967.40 |
|    GetOrAdd |       6 |   432.05 ns |    52.830 ns |  13.720 ns |   411.02 ns |   443.94 ns |  2,314,541.1 |  13,887,246.60 |
| AddOrUpdate |       6 |   472.97 ns |    51.991 ns |  13.502 ns |   449.17 ns |   481.87 ns |  2,114,303.9 |  12,685,823.40 |
|    Contains |       6 |    14.73 ns |     3.855 ns |   1.001 ns |    13.38 ns |    15.88 ns | 67,909,786.0 | 407,458,716.00 |
|         **Add** |       **7** |   **472.13 ns** |    **82.819 ns** |  **21.508 ns** |   **450.26 ns** |   **507.01 ns** |  **2,118,064.5** |  **14,826,451.50** |
| TryGetValue |       7 |    23.62 ns |     6.267 ns |   1.627 ns |    22.16 ns |    26.02 ns | 42,328,850.6 | 296,301,954.20 |
|      Remove |       7 |    37.92 ns |     2.324 ns |   0.603 ns |    37.02 ns |    38.62 ns | 26,373,550.6 | 184,614,854.20 |
|      Update |       7 |    33.57 ns |     8.929 ns |   2.319 ns |    31.13 ns |    36.78 ns | 29,787,596.8 | 208,513,177.60 |
|    GetOrAdd |       7 |   468.52 ns |    84.369 ns |  21.910 ns |   449.03 ns |   505.76 ns |  2,134,378.2 |  14,940,647.40 |
| AddOrUpdate |       7 |   485.41 ns |    89.374 ns |  23.210 ns |   466.84 ns |   524.50 ns |  2,060,096.7 |  14,420,676.90 |
|    Contains |       7 |    15.01 ns |     2.427 ns |   0.630 ns |    14.25 ns |    15.64 ns | 66,621,098.9 | 466,347,692.30 |
|         **Add** |       **8** |   **536.52 ns** |   **343.402 ns** |  **89.180 ns** |   **479.01 ns** |   **694.81 ns** |  **1,863,870.2** |  **14,910,961.60** |
| TryGetValue |       8 |    25.75 ns |     6.999 ns |   1.818 ns |    24.00 ns |    28.03 ns | 38,828,123.0 | 310,624,984.00 |
|      Remove |       8 |    39.76 ns |     8.586 ns |   2.230 ns |    37.60 ns |    42.98 ns | 25,148,246.9 | 201,185,975.20 |
|      Update |       8 |    35.38 ns |     8.407 ns |   2.183 ns |    32.11 ns |    37.63 ns | 28,267,819.2 | 226,142,553.60 |
|    GetOrAdd |       8 |   483.23 ns |    32.302 ns |   8.389 ns |   471.84 ns |   495.01 ns |  2,069,423.2 |  16,555,385.60 |
| AddOrUpdate |       8 |   559.73 ns |    56.984 ns |  14.798 ns |   540.13 ns |   575.04 ns |  1,786,570.0 |  14,292,560.00 |
|    Contains |       8 |    17.99 ns |     3.757 ns |   0.976 ns |    17.11 ns |    19.61 ns | 55,578,353.6 | 444,626,828.80 |
|         **Add** |       **9** |   **533.07 ns** |    **46.343 ns** |  **12.035 ns** |   **520.85 ns** |   **547.25 ns** |  **1,875,943.4** |  **16,883,490.60** |
| TryGetValue |       9 |    23.72 ns |     7.148 ns |   1.856 ns |    21.22 ns |    25.69 ns | 42,151,246.1 | 379,361,214.90 |
|      Remove |       9 |    36.07 ns |     8.614 ns |   2.237 ns |    34.20 ns |    39.56 ns | 27,723,637.2 | 249,512,734.80 |
|      Update |       9 |    31.24 ns |     4.801 ns |   1.247 ns |    29.98 ns |    33.33 ns | 32,014,558.0 | 288,131,022.00 |
|    GetOrAdd |       9 |   503.59 ns |   106.025 ns |  27.534 ns |   483.99 ns |   548.64 ns |  1,985,740.9 |  17,871,668.10 |
| AddOrUpdate |       9 |   608.48 ns |    35.039 ns |   9.099 ns |   599.27 ns |   620.26 ns |  1,643,430.3 |  14,790,872.70 |
|    Contains |       9 |    18.74 ns |     3.523 ns |   0.915 ns |    17.95 ns |    19.83 ns | 53,366,323.9 | 480,296,915.10 |
|         **Add** |      **10** |   **528.13 ns** |    **47.855 ns** |  **12.428 ns** |   **509.81 ns** |   **537.78 ns** |  **1,893,460.7** |  **18,934,607.00** |
| TryGetValue |      10 |    28.14 ns |     4.955 ns |   1.287 ns |    26.08 ns |    29.36 ns | 35,539,171.1 | 355,391,711.00 |
|      Remove |      10 |    45.34 ns |     9.787 ns |   2.542 ns |    42.25 ns |    48.08 ns | 22,056,958.4 | 220,569,584.00 |
|      Update |      10 |    37.66 ns |     6.522 ns |   1.694 ns |    36.75 ns |    40.68 ns | 26,556,500.2 | 265,565,002.00 |
|    GetOrAdd |      10 |   511.52 ns |    92.336 ns |  23.979 ns |   495.54 ns |   553.19 ns |  1,954,969.7 |  19,549,697.00 |
| AddOrUpdate |      10 |   629.57 ns |    58.096 ns |  15.087 ns |   613.05 ns |   653.87 ns |  1,588,382.8 |  15,883,828.00 |
|    Contains |      10 |    17.53 ns |     3.423 ns |   0.889 ns |    16.23 ns |    18.44 ns | 57,047,250.7 | 570,472,507.00 |

### Raw benchmark data for ConcurrentDictionary_B

``` ini

BenchmarkDotNet=v0.12.0, OS=Windows 8.1 (6.3.9600.0)
Intel Xeon CPU E5-2640 v3 2.60GHz, 2 CPU, 32 logical and 16 physical cores
Frequency=2533198 Hz, Resolution=394.7579 ns, Timer=TSC
.NET Core SDK=3.0.100
  [Host]    : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
  MediumRun : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT

Job=MediumRun  InvocationCount=1  IterationCount=5  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
|      Method | Threads |        Mean |        Error |     StdDev |      Median |         Min |         Max |         Op/s |     Op/s total |
|------------ |-------- |------------:|-------------:|-----------:|------------:|------------:|------------:|-------------:|--------------- |
|         **Add** |       **1** |   **110.27 ns** |    **24.197 ns** |   **6.284 ns** |   **113.21 ns** |    **99.35 ns** |   **114.78 ns** |  **9,068,695.2** |   **9,068,695.20** |
| TryGetValue |       1 |    14.15 ns |     1.074 ns |   0.279 ns |    14.07 ns |    13.84 ns |    14.56 ns | 70,686,151.8 |  70,686,151.80 |
|      Remove |       1 |    55.16 ns |    16.942 ns |   4.400 ns |    52.84 ns |    50.48 ns |    59.98 ns | 18,128,786.0 |  18,128,786.00 |
|      Update |       1 |    74.68 ns |    13.634 ns |   3.541 ns |    72.51 ns |    71.72 ns |    79.72 ns | 13,390,160.7 |  13,390,160.70 |
|    GetOrAdd |       1 |   103.96 ns |    22.429 ns |   5.825 ns |   101.45 ns |    98.94 ns |   113.66 ns |  9,618,671.7 |   9,618,671.70 |
| AddOrUpdate |       1 |   113.78 ns |     4.541 ns |   1.179 ns |   113.66 ns |   112.15 ns |   115.04 ns |  8,788,603.3 |   8,788,603.30 |
|    Contains |       1 |    14.11 ns |     2.234 ns |   0.580 ns |    13.86 ns |    13.50 ns |    14.78 ns | 70,863,720.1 |  70,863,720.10 |
|         **Add** |       **2** |   **173.44 ns** |    **70.558 ns** |  **18.324 ns** |   **178.90 ns** |   **141.68 ns** |   **187.54 ns** |  **5,765,833.3** |  **11,531,666.60** |
| TryGetValue |       2 |    15.12 ns |     8.525 ns |   2.214 ns |    14.10 ns |    13.99 ns |    19.07 ns | 66,141,216.3 | 132,282,432.60 |
|      Remove |       2 |    63.00 ns |    35.983 ns |   9.345 ns |    60.39 ns |    55.48 ns |    78.54 ns | 15,872,866.1 |  31,745,732.20 |
|      Update |       2 |    78.18 ns |    15.774 ns |   4.097 ns |    78.12 ns |    73.55 ns |    82.69 ns | 12,790,240.1 |  25,580,480.20 |
|    GetOrAdd |       2 |   172.92 ns |    12.604 ns |   3.273 ns |   172.62 ns |   169.80 ns |   178.00 ns |  5,782,874.5 |  11,565,749.00 |
| AddOrUpdate |       2 |   183.47 ns |    59.607 ns |  15.480 ns |   177.43 ns |   173.80 ns |   211.01 ns |  5,450,339.7 |  10,900,679.40 |
|    Contains |       2 |    13.32 ns |     0.353 ns |   0.092 ns |    13.30 ns |    13.21 ns |    13.47 ns | 75,101,132.5 | 150,202,265.00 |
|         **Add** |       **3** |   **271.82 ns** |   **129.868 ns** |  **33.726 ns** |   **269.73 ns** |   **218.38 ns** |   **303.61 ns** |  **3,678,885.1** |  **11,036,655.30** |
| TryGetValue |       3 |    15.03 ns |     4.024 ns |   1.045 ns |    14.38 ns |    14.27 ns |    16.69 ns | 66,525,013.3 | 199,575,039.90 |
|      Remove |       3 |    57.09 ns |    24.370 ns |   6.329 ns |    54.53 ns |    51.84 ns |    67.41 ns | 17,514,847.7 |  52,544,543.10 |
|      Update |       3 |    94.91 ns |   137.575 ns |  35.728 ns |    79.33 ns |    78.33 ns |   158.81 ns | 10,536,224.5 |  31,608,673.50 |
|    GetOrAdd |       3 |   263.61 ns |    82.476 ns |  21.419 ns |   262.70 ns |   232.64 ns |   286.97 ns |  3,793,519.0 |  11,380,557.00 |
| AddOrUpdate |       3 |   254.60 ns |    85.668 ns |  22.248 ns |   246.54 ns |   230.99 ns |   290.08 ns |  3,927,757.5 |  11,783,272.50 |
|    Contains |       3 |    14.86 ns |     3.870 ns |   1.005 ns |    15.01 ns |    13.82 ns |    16.34 ns | 67,308,630.5 | 201,925,891.50 |
|         **Add** |       **4** |   **285.52 ns** |    **59.363 ns** |  **15.416 ns** |   **282.02 ns** |   **269.85 ns** |   **307.25 ns** |  **3,502,436.2** |  **14,009,744.80** |
| TryGetValue |       4 |    17.05 ns |    11.807 ns |   3.066 ns |    15.81 ns |    15.14 ns |    22.44 ns | 58,668,203.9 | 234,672,815.60 |
|      Remove |       4 |    59.11 ns |     3.694 ns |   0.959 ns |    59.22 ns |    57.94 ns |    60.54 ns | 16,917,861.4 |  67,671,445.60 |
|      Update |       4 |    94.67 ns |    12.029 ns |   3.124 ns |    94.53 ns |    91.41 ns |    99.03 ns | 10,562,900.6 |  42,251,602.40 |
|    GetOrAdd |       4 |   277.09 ns |    23.071 ns |   5.992 ns |   275.05 ns |   269.37 ns |   284.34 ns |  3,608,889.1 |  14,435,556.40 |
| AddOrUpdate |       4 |   285.63 ns |    69.815 ns |  18.131 ns |   282.51 ns |   266.83 ns |   304.59 ns |  3,501,082.4 |  14,004,329.60 |
|    Contains |       4 |    15.50 ns |     3.809 ns |   0.989 ns |    14.93 ns |    14.76 ns |    17.02 ns | 64,501,162.1 | 258,004,648.40 |
|         **Add** |       **5** |   **444.27 ns** |   **127.472 ns** |  **33.104 ns** |   **437.32 ns** |   **409.83 ns** |   **495.82 ns** |  **2,250,885.8** |  **11,254,429.00** |
| TryGetValue |       5 |    16.39 ns |    10.742 ns |   2.790 ns |    14.52 ns |    14.46 ns |    20.69 ns | 60,997,027.2 | 304,985,136.00 |
|      Remove |       5 |    63.46 ns |    72.071 ns |  18.717 ns |    55.14 ns |    54.10 ns |    96.91 ns | 15,757,211.4 |  78,786,057.00 |
|      Update |       5 |    88.10 ns |     4.195 ns |   1.089 ns |    87.64 ns |    87.21 ns |    89.75 ns | 11,350,945.5 |  56,754,727.50 |
|    GetOrAdd |       5 |   413.65 ns |    32.018 ns |   8.315 ns |   416.87 ns |   399.87 ns |   420.95 ns |  2,417,517.1 |  12,087,585.50 |
| AddOrUpdate |       5 |   456.22 ns |   102.725 ns |  26.677 ns |   465.53 ns |   423.06 ns |   488.99 ns |  2,191,944.8 |  10,959,724.00 |
|    Contains |       5 |    19.66 ns |     8.739 ns |   2.269 ns |    19.66 ns |    16.45 ns |    21.94 ns | 50,866,244.9 | 254,331,224.50 |
|         **Add** |       **6** |   **495.51 ns** |   **290.902 ns** |  **75.546 ns** |   **472.13 ns** |   **435.66 ns** |   **623.14 ns** |  **2,018,112.6** |  **12,108,675.60** |
| TryGetValue |       6 |    19.83 ns |     6.640 ns |   1.724 ns |    20.28 ns |    18.05 ns |    21.98 ns | 50,427,515.5 | 302,565,093.00 |
|      Remove |       6 |    72.92 ns |    29.893 ns |   7.763 ns |    70.76 ns |    65.78 ns |    86.05 ns | 13,713,320.1 |  82,279,920.60 |
|      Update |       6 |   110.96 ns |    15.554 ns |   4.039 ns |   109.41 ns |   107.07 ns |   117.64 ns |  9,012,137.1 |  54,072,822.60 |
|    GetOrAdd |       6 |   468.17 ns |    41.473 ns |  10.770 ns |   473.47 ns |   455.40 ns |   479.77 ns |  2,135,971.6 |  12,815,829.60 |
| AddOrUpdate |       6 |   469.81 ns |    35.953 ns |   9.337 ns |   466.07 ns |   460.95 ns |   485.09 ns |  2,128,511.0 |  12,771,066.00 |
|    Contains |       6 |    18.30 ns |     5.115 ns |   1.328 ns |    18.46 ns |    16.93 ns |    19.76 ns | 54,633,899.5 | 327,803,397.00 |
|         **Add** |       **7** |   **502.25 ns** |    **83.288 ns** |  **21.630 ns** |   **501.35 ns** |   **480.37 ns** |   **537.24 ns** |  **1,991,026.4** |  **13,937,184.80** |
| TryGetValue |       7 |    21.08 ns |     5.642 ns |   1.465 ns |    21.48 ns |    19.15 ns |    22.40 ns | 47,441,594.1 | 332,091,158.70 |
|      Remove |       7 |    75.37 ns |    20.815 ns |   5.406 ns |    76.97 ns |    66.34 ns |    80.20 ns | 13,267,711.2 |  92,873,978.40 |
|      Update |       7 |   119.48 ns |    31.543 ns |   8.192 ns |   116.36 ns |   111.55 ns |   131.34 ns |  8,369,421.8 |  58,585,952.60 |
|    GetOrAdd |       7 |   508.25 ns |    58.261 ns |  15.130 ns |   503.31 ns |   491.44 ns |   531.39 ns |  1,967,536.8 |  13,772,757.60 |
| AddOrUpdate |       7 |   776.31 ns | 2,004.533 ns | 520.571 ns |   546.83 ns |   527.73 ns | 1,707.36 ns |  1,288,137.5 |   9,016,962.50 |
|    Contains |       7 |    21.81 ns |     7.704 ns |   2.001 ns |    21.71 ns |    19.98 ns |    25.10 ns | 45,841,374.1 | 320,889,618.70 |
|         **Add** |       **8** |   **518.81 ns** |    **46.207 ns** |  **12.000 ns** |   **522.62 ns** |   **504.59 ns** |   **534.33 ns** |  **1,927,502.1** |  **15,420,016.80** |
| TryGetValue |       8 |    21.73 ns |     3.911 ns |   1.016 ns |    21.41 ns |    20.52 ns |    23.19 ns | 46,029,540.2 | 368,236,321.60 |
|      Remove |       8 |    82.27 ns |    14.327 ns |   3.721 ns |    81.90 ns |    77.57 ns |    86.31 ns | 12,155,836.3 |  97,246,690.40 |
|      Update |       8 |   117.16 ns |    17.561 ns |   4.561 ns |   115.42 ns |   113.92 ns |   125.05 ns |  8,535,220.0 |  68,281,760.00 |
|    GetOrAdd |       8 |   544.55 ns |    95.863 ns |  24.895 ns |   541.57 ns |   515.83 ns |   577.50 ns |  1,836,377.4 |  14,691,019.20 |
| AddOrUpdate |       8 |   592.99 ns |    61.791 ns |  16.047 ns |   588.76 ns |   573.65 ns |   614.63 ns |  1,686,380.1 |  13,491,040.80 |
|    Contains |       8 |    24.17 ns |    12.953 ns |   3.364 ns |    23.86 ns |    20.07 ns |    27.57 ns | 41,374,127.8 | 330,993,022.40 |
|         **Add** |       **9** |   **584.82 ns** |    **31.576 ns** |   **8.200 ns** |   **587.23 ns** |   **573.66 ns** |   **595.45 ns** |  **1,709,917.4** |  **15,389,256.60** |
| TryGetValue |       9 |    26.86 ns |     7.798 ns |   2.025 ns |    27.11 ns |    24.60 ns |    28.86 ns | 37,230,277.9 | 335,072,501.10 |
|      Remove |       9 |    86.04 ns |    21.187 ns |   5.502 ns |    89.00 ns |    76.92 ns |    90.05 ns | 11,622,760.1 | 104,604,840.90 |
|      Update |       9 |   127.60 ns |     7.358 ns |   1.911 ns |   126.45 ns |   125.79 ns |   129.73 ns |  7,836,705.6 |  70,530,350.40 |
|    GetOrAdd |       9 |   548.94 ns |    61.996 ns |  16.100 ns |   546.68 ns |   527.84 ns |   565.35 ns |  1,821,703.1 |  16,395,327.90 |
| AddOrUpdate |       9 |   607.40 ns |    62.328 ns |  16.186 ns |   609.34 ns |   591.79 ns |   631.14 ns |  1,646,348.4 |  14,817,135.60 |
|    Contains |       9 |    25.12 ns |     8.877 ns |   2.305 ns |    24.92 ns |    22.44 ns |    27.85 ns | 39,805,472.8 | 358,249,255.20 |
|         **Add** |      **10** |   **601.10 ns** |    **66.669 ns** |  **17.314 ns** |   **603.57 ns** |   **580.72 ns** |   **618.89 ns** |  **1,663,611.0** |  **16,636,110.00** |
| TryGetValue |      10 |    27.52 ns |     5.137 ns |   1.334 ns |    27.15 ns |    26.29 ns |    29.59 ns | 36,337,432.9 | 363,374,329.00 |
|      Remove |      10 |    87.63 ns |    16.762 ns |   4.353 ns |    86.36 ns |    81.73 ns |    92.17 ns | 11,411,324.0 | 114,113,240.00 |
|      Update |      10 |   199.06 ns |   401.384 ns | 104.238 ns |   154.70 ns |   144.67 ns |   385.34 ns |  5,023,634.8 |  50,236,348.00 |
|    GetOrAdd |      10 |   828.97 ns | 2,074.896 ns | 538.844 ns |   578.64 ns |   569.43 ns | 1,792.02 ns |  1,206,316.7 |  12,063,167.00 |
| AddOrUpdate |      10 |   689.01 ns |    89.157 ns |  23.154 ns |   696.13 ns |   660.95 ns |   719.03 ns |  1,451,362.7 |  14,513,627.00 |
|    Contains |      10 |    27.09 ns |    10.285 ns |   2.671 ns |    27.46 ns |    24.05 ns |    31.01 ns | 36,915,321.2 | 369,153,212.00 |

