// Maksim Burtsev https://github.com/MBurtsev
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Benchmark.Helpers;
using Benchmark.ThreadsBench;
using BenchmarkDotNet.Running;

//using MBur.Collections.LockFree_A;
using MBur.Collections.LockFree_B;

namespace Benchmark
{
    public class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = ThreadsBenchConfig.Culture;

            // To configure, see the ThreadsBenchConfig
            // ----------------------------------------

            // ConcurrentDictionary
            //BenchmarkRunner.Run<ConcurrentDictionaryBench>();

            // LockFreeDictionary version A
            //BenchmarkRunner.Run<LockFreeDictionaryBench_A>();

            // LockFreeDictionary version B
            //BenchmarkRunner.Run<LockFreeDictionaryBench_B>();

            // Wat-Free Count
            // for this test recommended to configure ThreadsBenchConfig.OperationsCount = 100M 
            //BenchmarkRunner.Run<CountBench>();

            //TryGetTest();
            //Debug();
            //MemoryUsage();

            Console.WriteLine("Press any key for exit");
            Console.ReadLine();
        }

        static void Debug()
        {
            //var b = new LockFreeDictionaryBench_B();

            //b.Threads = 4;

            //b.AddSetup();
            //b.Add();

            //b.TryGetValueSetup();
            //b.TryGetValue();
        }

        static void TryGetTest()
        {
            var readers = 4;
            var writes  = 4;
            var cd = new ConcurrentDictionary<int, KeyValuePair<long, long>>();

            for (var threads = 0; threads < readers; ++threads)
            {
                Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            if (cd.TryGetValue(0, out KeyValuePair<long, long> item))
                            {
                                if (item.Key != item.Value)
                                {
                                    Console.WriteLine($"WRITER_DELAY must be increased {item.Key - item.Value}");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
            }

            for (var threads = 0; threads < writes; ++threads)
            {
                Task.Run(() =>
                {
                    Console.WriteLine($"Writer {Thread.CurrentThread.ManagedThreadId} begin");

                    try
                    {
                        for (long i = 0; ; i++)
                        {
                            cd[0] = new KeyValuePair<long, long>(i, i);
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });

                Thread.Sleep(1_000);
            }
        }

        #region ' Memory Usage '

        // The test shows the memory usage depending on the number of added items.

        static IDictionary dictionary;

        static void MemoryUsage()
        {
            Console.WriteLine($"Memory usage for LockFree.ConcurrentDictionary A");

            MemoryUsageLockFree_A(1);
            MemoryUsageLockFree_A(1000);
            MemoryUsageLockFree_A(1000_000);
            MemoryUsageLockFree_A(10_000_000);

            Console.WriteLine();

            Console.WriteLine($"Memory usage for LockFree.ConcurrentDictionary B");

            MemoryUsageLockFree_B(1);
            MemoryUsageLockFree_B(1000);
            MemoryUsageLockFree_B(1000_000);
            MemoryUsageLockFree_B(10_000_000);

            Console.WriteLine();

            Console.WriteLine($"Memory usage for Concurrent.ConcurrentDictionary");

            MemoryUsage(1);
            MemoryUsage(1000);
            MemoryUsage(1000_000);
            MemoryUsage(10_000_000);

            Console.WriteLine();
        }

        static void MemoryUsage(int count)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();

            var mem = GC.GetTotalMemory(true);

            dictionary = new System.Collections.Concurrent.ConcurrentDictionary<int, int>();

            for (var i = 0; i < count; ++i)
            {
                dictionary[i] = i;
            }

            mem = GC.GetTotalMemory(true) - mem;

            Console.WriteLine();
            Console.WriteLine($"Elements count: {count:### ### ### ###}");
            Console.WriteLine($"Memory alocated: {mem:### ### ### ###}");
            Console.WriteLine($"Size cost per item: {Math.Truncate((double)mem / count)}");

            dictionary = null;
        }


        static void MemoryUsageLockFree_A(int count)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();

            var mem = GC.GetTotalMemory(true);

            dictionary = new MBur.Collections.LockFree_A.ConcurrentDictionary<int, int>();

            for (var i = 0; i < count; ++i)
            {
                dictionary[i] = i;
            }

            mem = GC.GetTotalMemory(true) - mem;

            Console.WriteLine();
            Console.WriteLine($"Elements count: {count:### ### ### ###}");
            Console.WriteLine($"Memory alocated: {mem:### ### ### ###}");
            Console.WriteLine($"Size cost per item: {Math.Truncate((double)mem / count)}");

            dictionary = null;
        }

        static void MemoryUsageLockFree_B(int count)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();

            var mem = GC.GetTotalMemory(true);

            dictionary = new MBur.Collections.LockFree_B.ConcurrentDictionary<int, int>();

            for (var i = 0; i < count; ++i)
            {
                dictionary[i] = i;
            }

            mem = GC.GetTotalMemory(true) - mem;

            Console.WriteLine();
            Console.WriteLine($"Elements count: {count:### ### ### ###}");
            Console.WriteLine($"Memory alocated: {mem:### ### ### ###}");
            Console.WriteLine($"Size cost per item: {Math.Truncate((double)mem / count)}");

            dictionary = null;
        } 

        #endregion
    }
}
