// Maksim Burtsev https://github.com/MBurtsev
// Licensed under the MIT license.

using System;
using System.Threading;
using Benchmark.Helpers;
using Benchmark.ThreadsBench;
using BenchmarkDotNet.Running;

namespace Benchmark
{
    public class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = ThreadsBenchConfig.Culture;

            // ConcurrentDictionary
            //BenchmarkRunner.Run<ConcurrentDictionaryBench>();

            // LockFreeDictionary
            BenchmarkRunner.Run<LockFreeDictionaryBench>();

            // Wat-Free Count
            // for this test recommended to configure ThreadsBenchConfig.OperationsCount = 100M 
            //BenchmarkRunner.Run<CountBench>();


            Console.WriteLine("Complate");
            Console.ReadLine();
        }
    }
}
