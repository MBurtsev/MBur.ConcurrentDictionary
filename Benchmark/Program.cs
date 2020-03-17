﻿// Maksim Burtsev https://github.com/MBurtsev
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Benchmark.Helpers;
using Benchmark.ThreadsBench;
using BenchmarkDotNet.Running;
using MBur.Collections.LockFree;

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

            // LockFreeDictionary
            //BenchmarkRunner.Run<LockFreeDictionaryBench>();

            // Wat-Free Count
            // for this test recommended to configure ThreadsBenchConfig.OperationsCount = 100M 
            //BenchmarkRunner.Run<CountBench>();

            //TryGetTest();
            //Debug();
            MemoryUsage();

            Console.WriteLine("Complate");
            Console.ReadLine();
        }

        static void Debug()
        {
            var b = new LockFreeDictionaryBench();

            b.Threads = 4;

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

                Thread.Sleep(5_000);
            }
        }

        static void MemoryUsage()
        {
            MemoryUsage(new MBur.Collections.LockFree.ConcurrentDictionary<int, int>());
            MemoryUsage(new System.Collections.Concurrent.ConcurrentDictionary<int, int>());
        }

        static void MemoryUsage(IDictionary dictionary)
        {
            Console.WriteLine($"Memory usage for {dictionary.GetType().FullName}");

            MemoryUsage(dictionary, 1);
            MemoryUsage(dictionary, 256);
            MemoryUsage(dictionary, 1000);
            MemoryUsage(dictionary, 1000_000);
            MemoryUsage(dictionary, 10_000_000);

            Console.WriteLine();
        }

        static void MemoryUsage(IDictionary dictionary, int count)
        {
            dictionary.Clear();

            var size = GC.GetTotalMemory(true);

            for (var i = 0; i < count; ++i)
            {
                dictionary[i] = i;
            }

            var mem = GC.GetTotalMemory(true) - size;

            Console.WriteLine();
            Console.WriteLine($"Elements count: {count:### ### ### ###}");
            Console.WriteLine($"Memory alocated: {mem:### ### ### ###}");
            Console.WriteLine($"Size cost per item: {Math.Truncate((double)mem / count)}");
        }
    }
}
