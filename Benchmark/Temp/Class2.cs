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
using System.Collections.Generic;
using Benchmark.Helpers;

namespace Benchmark
{
    [Config(typeof(ThreadsBenchConfig))]
    [BenchmarkCategory("Collections")]
    public class Class2
    {
        public const int operationsCount = 10_000_000;

        object data;

        [Params(/**/1/*, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16*/)]
        public int Threads { get; set; }


        #region '  Push '

        [IterationSetup(Target = nameof(PushTest))]
        public void PushSetup()
        {
            data = new List<int>();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void PushTest()
        {
            var data = (List<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Add(i);
            }
        }

        #endregion

        #region '  Push with capacity '

        [IterationSetup(Target = nameof(PushTest_capacity))]
        public void PushSetup_capacity()
        {
            data = new List<int>(operationsCount);
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void PushTest_capacity()
        {
            var data = (List<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Add(i);
            }
        }

        #endregion
    }
}
