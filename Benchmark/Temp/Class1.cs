using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using Nim.Collections.LockFree;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;
using System.Collections.Concurrent;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Globalization;
using BenchmarkDotNet.Engines;
using System.Threading;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System.Runtime.CompilerServices;
using Benchmark.Helpers;

namespace Benchmark
{
    [Config(typeof(ThreadsBenchConfig))]
    [BenchmarkCategory("Collections")]
    public class Class1
    {
        public const int operationsCount = 1_000_0;

        ThreadsBenchHelper test;
        object data;

        [Params(/**/1/*, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16*/)]
        public int Threads { get; set; }
        
        #region ' SolidStack Push with capacity '

        [IterationSetup(Target = nameof(SolidStackPushTest_capacity))]
        public void SolidStackPushSetup_capacity()
        {
            data = new SolidStack<int>(Threads * operationsCount);

            test = new ThreadsBenchHelper();

            Thread.Sleep(1000);

            test.AddWorks(SolidStackPushWork_capacity, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void SolidStackPushTest_capacity()
        {
            test.Begin();
        }

        public void SolidStackPushWork_capacity()
        {
            var data = (SolidStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
            }
        }

        #endregion
    }
}
