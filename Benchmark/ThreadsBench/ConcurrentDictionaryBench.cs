// Maksim Burtsev https://github.com/MBurtsev
// Licensed under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;
using Benchmark.Helpers;
using System.Collections.Generic;
using System.Threading;

namespace Benchmark.ThreadsBench
{
    [Config(typeof(ThreadsBenchConfig))]
    public class ConcurrentDictionaryBench
    {
        // This number is necessary to calculate the base key.
        // It should be more than the number of operations.
        const  int threads_key_range = 50_000_000;
        int thread_id;
        ConcurrentDictionary<int, int> data;
        ThreadsBenchHelper bench;

        [ParamsSource(nameof(ThreadValues))]
        public int Threads { get; set; }

        #region ' TryAdd '

        [IterationSetup(Target = nameof(Add))]
        public void AddSetup()
        {
            thread_id = 0;

            data = new ConcurrentDictionary<int, int>();
            bench = new ThreadsBenchHelper();

            bench.AddWorks(AddWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void Add()
        {
            bench.Begin();
        }

        void AddWork()
        {
            var key = Interlocked.Add(ref thread_id, 1) * threads_key_range;

            for (int i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.TryAdd(key + i, i);
            }
        }

        #endregion

        #region ' TryGetValue '

        [IterationSetup(Target = nameof(TryGetValue))]
        public void TryGetValueSetup()
        {
            thread_id = 0;

            data = new ConcurrentDictionary<int, int>();

            // Preparing data so that the keys are different for each thread.
            for (var i = 0; i < Threads; ++i)
            {
                var key = (i + 1) * threads_key_range;

                for (var j = 0; j < ThreadsBenchConfig.OperationsCount; ++j)
                {
                    data.TryAdd(key + j, j);
                }
            }

            bench = new ThreadsBenchHelper();

            bench.AddWorks(TryGetValueWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void TryGetValue()
        {
            bench.Begin();
        }

        void TryGetValueWork()
        {
            var key = Interlocked.Add(ref thread_id, 1) * threads_key_range;

            for (int i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.TryGetValue(key + i, out _);
            }
        }

        #endregion

        #region ' TryRemove '

        [IterationSetup(Target = nameof(Remove))]
        public void RemoveSetup()
        {
            thread_id = 0;

            data = new ConcurrentDictionary<int, int>();

            // Preparing data so that the keys are different for each thread.
            for (var i = 0; i < Threads; ++i)
            {
                var key = (i + 1) * threads_key_range;

                for (var j = 0; j < ThreadsBenchConfig.OperationsCount; ++j)
                {
                    data.TryAdd(key + j, j);
                }
            }

            bench = new ThreadsBenchHelper();

            bench.AddWorks(RemoveWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void Remove()
        {
            bench.Begin();
        }

        void RemoveWork()
        {
            var key = Interlocked.Add(ref thread_id, 1) * threads_key_range;

            for (int i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.TryRemove(key + i, out int val);
            }
        }

        #endregion

        #region ' TryUpdate '

        [IterationSetup(Target = nameof(Update))]
        public void UpdateSetup()
        {
            thread_id = 0;

            data = new ConcurrentDictionary<int, int>();

            // Preparing data so that the keys are different for each thread.
            for (var i = 0; i < Threads; ++i)
            {
                var key = (i + 1) * threads_key_range;

                for (var j = 0; j < ThreadsBenchConfig.OperationsCount; ++j)
                {
                    data.TryAdd(key + j, j);
                }
            }

            bench = new ThreadsBenchHelper();

            bench.AddWorks(UpdateWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void Update()
        {
            bench.Begin();
        }

        void UpdateWork()
        {
            var key = Interlocked.Add(ref thread_id, 1) * threads_key_range;

            for (int i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.TryUpdate(key + i, i + 1, i);
            }
        }

        #endregion

        #region ' GetOrAdd '

        [IterationSetup(Target = nameof(GetOrAdd))]
        public void GetOrAddSetup()
        {
            thread_id = 0;

            data = new ConcurrentDictionary<int, int>();
            bench = new ThreadsBenchHelper();

            bench.AddWorks(GetOrAddWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void GetOrAdd()
        {
            bench.Begin();
        }

        void GetOrAddWork()
        {
            var key = Interlocked.Add(ref thread_id, 1) * threads_key_range;

            // add mode
            for (int i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.GetOrAdd(key + i, i);
            }
        }

        #endregion

        #region ' AddOrUpdate '

        [IterationSetup(Target = nameof(AddOrUpdate))]
        public void AddOrUpdateSetup()
        {
            thread_id = 0;

            data = new ConcurrentDictionary<int, int>();
            bench = new ThreadsBenchHelper();

            bench.AddWorks(AddOrUpdateWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void AddOrUpdate()
        {
            bench.Begin();
        }

        void AddOrUpdateWork()
        {
            var key = Interlocked.Add(ref thread_id, 1) * threads_key_range;

            // add mode
            for (int i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.AddOrUpdate(key + i, i, AddOrUpdateFactory);
            }

            // update mode
            //for (int i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            //{
            //    data.AddOrUpdate(key + i, i, AddOrUpdateFactory);
            //}
        }

        int AddOrUpdateFactory(int key, int val)
        {
            return -1;
        }

        #endregion

        #region ' ContainsKey '

        [IterationSetup(Target = nameof(Contains))]
        public void ContainsSetup()
        {
            thread_id = 0;

            data = new ConcurrentDictionary<int, int>();

            // Preparing data so that the keys are different for each thread.
            for (var i = 0; i < Threads; ++i)
            {
                var key = (i + 1) * threads_key_range;

                for (var j = 0; j < ThreadsBenchConfig.OperationsCount; ++j)
                {
                    data.TryAdd(key + j, j);
                }
            }

            bench = new ThreadsBenchHelper();

            bench.AddWorks(ContainsWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void Contains()
        {
            bench.Begin();
        }

        void ContainsWork()
        {
            var key = Interlocked.Add(ref thread_id, 1) * threads_key_range;

            for (int i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.ContainsKey(key + i);
            }
        }

        #endregion

        #region ' Helpers '

        public IEnumerable<int> ThreadValues => ThreadsBenchConfig.Threads;

        #endregion
    }
}
