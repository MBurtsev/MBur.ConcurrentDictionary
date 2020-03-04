// Maksim Burtsev https://github.com/MBurtsev
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Benchmark.Helpers;
using BenchmarkDotNet.Attributes;

namespace Benchmark.ThreadsBench
{
    [Config(typeof(ThreadsBenchConfig))]
    public class CountBench
    {
        ThreadsBenchHelper bench;

        [ParamsSource(nameof(ThreadValues))]
        public int Threads { get; set; }

        #region ' Interlocked.Add Count '

        int interlocked_value = 0;

        [IterationSetup(Target = nameof(InterlockedAddBench))]
        public void InterlockedAddSetup()
        {
            interlocked_value = 0;

            bench = new ThreadsBenchHelper();

            bench.AddWorks(InterlockedAddWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void InterlockedAddBench()
        {
            bench.Begin();
        }

        void InterlockedAddWork()
        {
            for (int i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                Interlocked.Add(ref interlocked_value, 1);
            }
        }

        #endregion

        #region ' Wait-Free Count '

        // thread id
        [ThreadStatic]
        static int Wait_Free_Id;
        // lock in resizing
        int countLock;
        // count values
        ConcurrentDictionaryCounter[] counts;
        
        // prepare bench
        [IterationSetup(Target = nameof(WaitFreeBench))]
        public void WaitFreeSetup()
        {
            counts = new ConcurrentDictionaryCounter[16];

            // counts initialization
            for (var i = 0; i < counts.Length; ++i)
            {
                counts[i] = new ConcurrentDictionaryCounter();
            }

            bench = new ThreadsBenchHelper();

            bench.AddWorks(WaitFreeAddWork, Threads);
            bench.Prepare();
        }

        // bench method
        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void WaitFreeBench()
        {
            bench.Begin();
        }

        // threads working method
        void WaitFreeAddWork()
        {
            var id = Wait_Free_Id;

            for (int i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                if (id == 0 || id >= counts.Length)
                {
                    id = Wait_Free_Id = Thread.CurrentThread.ManagedThreadId;

                    if (id >= counts.Length)
                    {
                        CountsResize();

                        counts = Volatile.Read(ref counts);
                    }
                }

                counts[id].Count++;
            }
        }

        // grow counts array
        void CountsResize()
        {
            var id  = Wait_Free_Id;
            var loc = Interlocked.CompareExchange(ref countLock, id, 0);

            if (loc != 0)
            {
                while (Volatile.Read(ref countLock) != 0)
                {
                    Thread.Yield();
                }

                if (id >= Volatile.Read(ref counts).Length)
                {
                    CountsResize();
                }

                return;
            }

            var cnt = counts;
            var len = cnt.Length;

            if (id > cnt.Length)
            {
                len = id;
            }

            var tmp_counts = new ConcurrentDictionaryCounter[len * 2];

            Array.Copy(cnt, tmp_counts, cnt.Length);

            // fill with empty counts
            for (var i = cnt.Length; i < tmp_counts.Length; ++i)
            {
                tmp_counts[i] = new ConcurrentDictionaryCounter();
            }

            // write new counts link
            counts = tmp_counts;

            // unlock
            countLock = 0;
        }

        // counts item
        [DebuggerDisplay("Count = {Count}")]
        [StructLayout(LayoutKind.Explicit, Size = PaddingHelpers.CACHE_LINE_SIZE)]
        internal class ConcurrentDictionaryCounter
        {
            [FieldOffset(0)]
            public long Count;
        }

        #endregion

        #region ' Helpers '

        public IEnumerable<int> ThreadValues => ThreadsBenchConfig.Threads;

        static class PaddingHelpers
        {
        #if ARM64
            internal const int CACHE_LINE_SIZE = 128;
        #else
            internal const int CACHE_LINE_SIZE = 64;
        #endif
        }

        #endregion
    }
}
