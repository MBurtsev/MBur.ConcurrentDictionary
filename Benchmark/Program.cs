// Maksim Burtsev https://github.com/MBurtsev
// Licensed under the MIT license.

using System;
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

            TryGetTest();
            //Debug();

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
            var writes  = 2;
            var cd = new ConcurrentDictionary<int, KeyValuePair<long, long>>();

            for (var threads = 0; threads < writes; ++threads)
            {
                Task.Run(() =>
                {
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
            }

            for (var threads = 0; threads < readers; ++threads)
            {
                Task.Run(() =>
                {
                    try
                    { 
                        while (true)
                        {
                            cd.TryGetValue(0, out KeyValuePair<long, long> item);

                            if (item.Key != item.Value)
                            {
                                Console.WriteLine($"WRITER_DELAY must be increased {item.Key} != {item.Value}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
            }
        }
    }
}
