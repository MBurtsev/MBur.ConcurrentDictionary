using System;
using BenchmarkDotNet.Attributes;
using Nim.Collections.LockFree;
using System.Collections.Concurrent;
using Benchmark.Helpers;

namespace Benchmark
{
    [Config(typeof(ThreadsBenchConfig))]
    [BenchmarkCategory("Collections")]
    public class StackBench
    {
        const int operationsCount = 100_000;

        ThreadsBenchHelper test;
        object data;

        [Params(/**/1, 2, 3, 4, 5, 6, 7, 8, 9, 10/*, 11, 12, 13, 14, 15, 16*/)]
        public int Threads { get; set; }

        #region ' SegmentStack Push '

        [IterationSetup(Target = nameof(SegmentStackPushTest))]
        public void SegmentStackPushSetup()
        {
            data = new SegmentStack<int>();
            test = new ThreadsBenchHelper();

            test.AddWorks(SegmentStackPushWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void SegmentStackPushTest()
        {
            test.Begin();
        }

        public void SegmentStackPushWork()
        {
            var data = (SegmentStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
            }
        }

        #endregion

        #region ' SegmentStack Pop '

        [IterationSetup(Target = nameof(SegmentStackPopTest))]
        public void SegmentStackPopSetup()
        {
            var tmp = new SegmentStack<int>();

            for (var i = 0; i < operationsCount * Threads; ++i)
            {
                tmp.Push(i);
            }

            data = tmp;
            test = new ThreadsBenchHelper();

            test.AddWorks(SegmentStackPopWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void SegmentStackPopTest()
        {
            test.Begin();
        }

        public void SegmentStackPopWork()
        {
            var data = (SegmentStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.TryPop(out _);
            }
        }

        #endregion

        #region ' SegmentStack Peek '

        [IterationSetup(Target = nameof(SegmentStackPeekTest))]
        public void SegmentStackPeekSetup()
        {
            var tmp = new SegmentStack<int>();

            for (var i = 0; i < operationsCount; ++i)
            {
                tmp.Push(i);
            }

            data = tmp;
            test = new ThreadsBenchHelper();

            test.AddWorks(SegmentStackPeekWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void SegmentStackPeekTest()
        {
            test.Begin();
        }

        public void SegmentStackPeekWork()
        {
            var data = (SegmentStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.TryPeek(out _);
            }
        }

        #endregion

        #region ' SegmentStack Push Peek Pop '

        [IterationSetup(Target = nameof(SegmentStackPushPeekPopTest))]
        public void SegmentStackPushPeekPopSetup()
        {
            var tmp = new SegmentStack<int>();

            for (var i = 0; i < operationsCount; ++i)
            {
                tmp.Push(i);
            }

            data = tmp;
            test = new ThreadsBenchHelper();

            test.AddWorks(SegmentStackPushPeekPopWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void SegmentStackPushPeekPopTest()
        {
            test.Begin();
        }

        public void SegmentStackPushPeekPopWork()
        {
            var data = (SegmentStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
                data.TryPeek(out _);
                data.TryPop(out _);
            }
        }

        #endregion


        #region ' SolidStack Push '

        [IterationSetup(Target = nameof(SolidStackPushTest))]
        public void SolidStackPushSetup()
        {
            data = new SolidStack<int>();
            test = new ThreadsBenchHelper();

            test.AddWorks(SolidStackPushWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void SolidStackPushTest()
        {
            test.Begin();
        }

        public void SolidStackPushWork()
        {
            var data = (SolidStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
            }
        }

        #endregion

        #region ' SolidStack Push with capacity '

        [IterationSetup(Target = nameof(SolidStackPushTestCapacity))]
        public void SolidStackPushSetup_capacity()
        {
            data = new SolidStack<int>(Threads * operationsCount);

            test = new ThreadsBenchHelper();

            test.AddWorks(SolidStackPushWorkCapacity, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void SolidStackPushTestCapacity()
        {
            test.Begin();
        }

        public void SolidStackPushWorkCapacity()
        {
            var data = (SolidStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
            }
        }

        #endregion

        #region ' SolidStack Pop '

        [IterationSetup(Target = nameof(SolidStackPopTest))]
        public void SolidStackPopSetup()
        {
            var tmp = new SolidStack<int>();

            for (var i = 0; i < operationsCount * Threads; ++i)
            {
                tmp.Push(i);
            }

            data = tmp;
            test = new ThreadsBenchHelper();

            test.AddWorks(SolidStackPopWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void SolidStackPopTest()
        {
            test.Begin();
        }

        public void SolidStackPopWork()
        {
            var data = (SolidStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.TryPop(out _);
            }
        }

        #endregion

        #region ' SolidStack Peek '

        [IterationSetup(Target = nameof(SolidStackPeekTest))]
        public void SolidStackPeekSetup()
        {
            var tmp = new SolidStack<int>();

            for (var i = 0; i < operationsCount; ++i)
            {
                tmp.Push(i);
            }

            data = tmp;
            test = new ThreadsBenchHelper();

            test.AddWorks(SolidStackPeekWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void SolidStackPeekTest()
        {
            test.Begin();
        }

        public void SolidStackPeekWork()
        {
            var data = (SolidStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.TryPeek(out _);
            }
        }

        #endregion

        #region ' SolidStack Push Peek Pop '

        [IterationSetup(Target = nameof(SolidStackPushPeekPopTest))]
        public void SolidStackPushPeekPopSetup()
        {
            var tmp = new SolidStack<int>();

            for (var i = 0; i < operationsCount; ++i)
            {
                tmp.Push(i);
            }

            data = tmp;
            test = new ThreadsBenchHelper();

            test.AddWorks(SolidStackPushPeekPopWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void SolidStackPushPeekPopTest()
        {
            test.Begin();
        }

        public void SolidStackPushPeekPopWork()
        {
            var data = (SolidStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
                data.TryPeek(out _);
                data.TryPop(out _);
            }
        }

        #endregion


        #region ' ConcurrentStack Push '

        [IterationSetup(Target = nameof(ConcurrentStackPushTest))]
        public void ConcurrentStackPushSetup()
        {
            data = new ConcurrentStack<int>();
            test = new ThreadsBenchHelper();

            test.AddWorks(ConcurrentStackPushWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void ConcurrentStackPushTest()
        {
            test.Begin();
        }

        public void ConcurrentStackPushWork()
        {
            var data = (ConcurrentStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
            }
        }

        #endregion

        #region ' ConcurrentStack Pop '

        [IterationSetup(Target = nameof(ConcurrentStackPopTest))]
        public void ConcurrentStackPopSetup()
        {
            var tmp = new ConcurrentStack<int>();

            for (var i = 0; i < operationsCount * Threads; ++i)
            {
                tmp.Push(i);
            }

            data = tmp;
            test = new ThreadsBenchHelper();

            test.AddWorks(ConcurrentStackPopWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void ConcurrentStackPopTest()
        {
            test.Begin();
        }

        public void ConcurrentStackPopWork()
        {
            var data = (ConcurrentStack<int>)this.data;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.TryPop(out _);
            }
        }

        #endregion

        #region ' ConcurrentStack Peek '

        [IterationSetup(Target = nameof(ConcurrentStackPeekTest))]
        public void ConcurrentStackPeekSetup()
        {
            var tmp = new ConcurrentStack<int>();

            for (var i = 0; i < operationsCount; ++i)
            {
                tmp.Push(i);
            }

            data = tmp;
            test = new ThreadsBenchHelper();

            test.AddWorks(ConcurrentStackPeekWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void ConcurrentStackPeekTest()
        {
            test.Begin();
        }

        public void ConcurrentStackPeekWork()
        {
            var data = (ConcurrentStack<int>)this.data;
            var sum = 0L;

            for (var i = 0; i < operationsCount; ++i)
            {
                data.TryPeek(out int val);

                sum += val;
            }

            String.Concat(sum);
        }

        #endregion

        #region ' ConcurrentStack Push Peek Pop '

        [IterationSetup(Target = nameof(ConcurrentStackPushPeekPopTest))]
        public void ConcurrentStackPushPeekPopSetup()
        {
            var tmp = new ConcurrentStack<int>();

            for (var i = 0; i < operationsCount; ++i)
            {
                tmp.Push(i);
            }

            data = tmp;
            test = new ThreadsBenchHelper();

            test.AddWorks(ConcurrentStackPushPeekPopWork, Threads);
            test.Prepare();
        }

        [Benchmark(OperationsPerInvoke = operationsCount)]
        public void ConcurrentStackPushPeekPopTest()
        {
            test.Begin();
        }

        public void ConcurrentStackPushPeekPopWork()
        {
            var data = (ConcurrentStack<int>)this.data;
            var sum = 0L; 

            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
                data.TryPeek(out _);
                data.TryPop(out int val);

                sum += val;
            }

            String.Concat(sum);
        }

        #endregion
    }
}
