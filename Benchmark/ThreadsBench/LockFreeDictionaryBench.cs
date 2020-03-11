// Maksim Burtsev https://github.com/MBurtsev
// Licensed under the MIT license.

#define TRY_ADD
//#define TRY_GET_VALUE
//#define TRY_REMOVE
//#define TRY_UPDATE
//#define GET_OR_ADD
//#define ADD_OR_UPDATE
//#define CONTAINS_KEY

using System;
using BenchmarkDotNet.Attributes;
using Benchmark.Helpers;
using System.Collections.Generic;
using System.Threading;
using MBur.Collections.LockFree;

namespace Benchmark.ThreadsBench
{
    [Config(typeof(ThreadsBenchConfig))]
    public class LockFreeDictionaryBench
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

#if TRY_ADD

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

#endif

        #endregion

        #region ' TryGetValue '

#if TRY_GET_VALUE

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

#endif

        #endregion

        #region ' TryRemove '

#if TRY_REMOVE

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

#endif

        #endregion

        #region ' TryUpdate '

#if TRY_UPDATE

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

#endif
        #endregion

        #region ' GetOrAdd '

#if GET_OR_ADD

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

#endif

        #endregion

        #region ' AddOrUpdate '

#if ADD_OR_UPDATE

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

#endif

        #endregion

        #region ' ContainsKey ' 

#if CONTAINS_KEY

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
#endif
        #endregion

        #region ' Helpers '

        public IEnumerable<int> ThreadValues => ThreadsBenchConfig.Threads;

        #endregion
    }
}
