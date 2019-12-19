// Maksim Burtsev https://github.com/nim
// Licensed under the MIT license.

using Benchmark.Helpers;
using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;

namespace Benchmark.GenericBench
{
    [Config(typeof(GenericBenchConfig))]
    public class SystemStackBench
    {
        const int operationsCount = 50_000_000;
        Stack<int> data;

        #region ' Push '

        [IterationSetup(Target = nameof(Push))]
        public void PushSetup()
        {
            data = new Stack<int>();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void Push()
        {
            var data = this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
            }
        }

        #endregion

        #region ' Pop '

        [IterationSetup(Target = nameof(Pop))]
        public void PopSetup()
        {
            data = new Stack<int>();

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
            }
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void Pop()
        {
            var data = this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Pop();
            }
        }

        #endregion

        #region ' Peek '

        #endregion

        #region ' Push Peek Pop '

        [IterationSetup(Target = nameof(PushPeekPop))]
        public void PushPeekPopSetup()
        {
            data = new Stack<int>();

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
            }
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void PushPeekPop()
        {
            var sum = 0L;
            var data = this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);

                sum += data.Peek();
                sum += data.Pop();
            }

            // Anti cutting during optimization 
            String.Concat(sum);
        }

        #endregion

    }
}
