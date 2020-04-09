Lock-Free hash table is presented to your attention. The implementation is much better than the .Net Core version. The API is fully compatible with the current version 3.0. The hash table is presented in two versions A and B. In the repository, you can find unit tests proving the absence of bugs. Benchmarks of performance and memory usage are also presented. Below is a list of advantages and disadvantages. ![See for more information](https://www.linkedin.com/pulse/lock-free-hash-table-maksim-burtsev/)

## ConcurrentDictionary_A

### Advantage

* TryAdd faster from 400% to 450%
* TryRemove faster from 60% to 420%
* TryUpdate faster from 60% to 250%
* GetOrUpdate faster from 400% to 480%
* AddOrUpdate faster from 400% to 650%
* ContainsKey faster from 15% to 60%
* Functions GetValues, GetKeys, CopyTo and others do not block the table.
* Functions with callbacks like "TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)" only calls the factory if the value is actually used. The current version can call the factory even if the received value will not used.
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
* Functions GetValues, GetKeys, CopyTo and others do not block the table.
* Functions with callbacks like "TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)" only calls the factory if the value is actually used. The current version can call the factory even if the received value will not used.
* Memory usage overage 2,5 times less

### Disadvantage

* TryUpdate slower from 9% to 48%

![1](https://i.imgur.com/09OU81J.png)

![2](https://i.imgur.com/gixyx4L.png)

![3](https://i.imgur.com/1qkqgC0.png)

![4](https://i.imgur.com/hWQCqTv.png)

![5](https://i.imgur.com/fjcanEs.png)

![6](https://i.imgur.com/yl9VIER.png)

![7](https://i.imgur.com/yoKubjS.png)

![8](https://i.imgur.com/ryvAe3y.png)
