// Maksim Burtsev https://github.com/nim
// Licensed under the MIT license.

using System;
using System.Threading;
using Benchmark.GenericBench;
using Benchmark.Helpers;
using Benchmark.ThreadsBench;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Configuration;

namespace Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = ThreadsBenchConfig.Culture;

            // Concurrent
            //BenchmarkRunner.Run<SegmentStackBench>();
            BenchmarkRunner.Run<SolidStackBench>();
            //BenchmarkRunner.Run<ConcurrentStackBench>();

            // Generic
            //BenchmarkRunner.Run<NimStackBench>();
            //BenchmarkRunner.Run<SystemStackBench>();

            Console.ReadKey();
        }
    }
}
