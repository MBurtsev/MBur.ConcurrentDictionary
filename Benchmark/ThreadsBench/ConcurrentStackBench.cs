// Maksim Burtsev https://github.com/nim
// Licensed under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using System.Collections.Concurrent;
using Benchmark.Helpers;
using System.Collections.Generic;

namespace Benchmark.ThreadsBench
{
    [Config(typeof(ThreadsBenchConfig))]
    public class ConcurrentStackBench
    {
        ConcurrentStack<int> data;
        ThreadsBenchHelper bench;

        [ParamsSource(nameof(ThreadValues))]
        public int Threads { get; set; }

        #region ' Push '

        [IterationSetup(Target = nameof(Push))]
        public void PushSetup()
        {
            data = new ConcurrentStack<int>();
            bench = new ThreadsBenchHelper();

            bench.AddWorks(PushWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void Push()
        {
            bench.Begin();
        }

        public void PushWork()
        {
            var data = this.data;

            for (var i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.Push(i);
            }
        }

        #endregion

        #region ' Pop '

        [IterationSetup(Target = nameof(Pop))]
        public void PopSetup()
        {
            data = new ConcurrentStack<int>();

            for (var i = 0; i < ThreadsBenchConfig.OperationsCount * Threads; ++i)
            {
                data.Push(i);
            }

            bench = new ThreadsBenchHelper();

            bench.AddWorks(PopWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void Pop()
        {
            bench.Begin();
        }

        public void PopWork()
        {
            var data = this.data;

            for (var i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.TryPop(out _);
            }
        }

        #endregion

        #region ' Peek '

        [IterationSetup(Target = nameof(Peek))]
        public void PeekSetup()
        {
            data = new ConcurrentStack<int>();

            for (var i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.Push(i);
            }

            bench = new ThreadsBenchHelper();

            bench.AddWorks(PeekWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount)]
        public void Peek()
        {
            bench.Begin();
        }

        public void PeekWork()
        {
            var sum = 0L;
            var data = this.data;

            for (var i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.TryPeek(out int val);

                sum += val;
            }

            // Anti cutting during optimization 
            String.Concat(sum);
        }

        #endregion

        #region ' Push Peek Pop '

        [IterationSetup(Target = nameof(PushPeekPop))]
        public void PushPeekPopSetup()
        {
            data = new ConcurrentStack<int>();

            for (var i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.Push(i);
            }

            bench = new ThreadsBenchHelper();

            bench.AddWorks(PushPeekPopWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount * 2)]
        public void PushPeekPop()
        {
            bench.Begin();
        }

        public void PushPeekPopWork()
        {
            var sum = 0L;
            var data = this.data;

            for (var i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.Push(i);
                data.TryPeek(out int val);

                sum += val;

                data.TryPop(out val);

                sum += val;
            }

            // Anti cutting during optimization 
            String.Concat(sum);
        }

        #endregion

        #region ' Push Then Pop '

        [IterationSetup(Target = nameof(PushThenPop))]
        public void PushThenPopSetup()
        {
            data = new ConcurrentStack<int>();

            for (var i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.Push(i);
            }

            bench = new ThreadsBenchHelper();

            bench.AddWorks(PushThenPopWork, Threads);
            bench.Prepare();
        }

        [Benchmark(OperationsPerInvoke = ThreadsBenchConfig.OperationsCount * 2)]
        public void PushThenPop()
        {
            bench.Begin();
        }

        public void PushThenPopWork()
        {
            var sum = 0L;
            var data = this.data;

            // push 
            for (var i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.Push(i);
            }

            // pop
            for (var i = 0; i < ThreadsBenchConfig.OperationsCount; ++i)
            {
                data.TryPop(out int val);

                sum += val;
            }

            // Anti cutting during optimization 
            String.Concat(sum);
        }

        #endregion

        public IEnumerable<int> ThreadValues => ThreadsBenchConfig.Threads;
    }
}
