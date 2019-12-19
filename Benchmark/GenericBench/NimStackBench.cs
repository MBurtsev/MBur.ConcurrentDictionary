// Maksim Burtsev https://github.com/nim
// Licensed under the MIT license.

using Benchmark.Helpers;
using BenchmarkDotNet.Attributes;
using Nim.Collections.Generic;
using System;

namespace Benchmark.GenericBench
{
    [Config(typeof(GenericBenchConfig))]
    public class NimStackBench
    {
        const int operationsCount = 10_000_000;
        SegmentStack<int> data;

        #region ' Push '

        [IterationSetup(Target = nameof(Push))]
        public void PushSetup()
        {
            data = new SegmentStack<int>(operationsCount);
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
            data = new SegmentStack<int>();

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
            data = new SegmentStack<int>();

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
