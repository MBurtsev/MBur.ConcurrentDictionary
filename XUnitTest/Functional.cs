using MBur.Collections.LockFree;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTest
{
    public class Functional
    {
        private const int operations = 1_000_000;

        [Fact]
        public static void TryAdd()
        {
            TryAddTest(1);
            TryAddTest(2);
            TryAddTest(4);
        }

        public static void TryAddTest(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, long>();
            var cts = new CancellationTokenSource();

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < operations; ++j)
                        {
                            cd.TryAdd(j, j);
                        }

                        Interlocked.Add(ref ready, 1);
                    }
                    catch (Exception e)
                    {
                        exception = e;

                        ready = threads;

                        cts.Cancel();
                    }
                }, cts.Token);
            }
            
            while (Volatile.Read(ref ready) != threads)
            {
                Thread.Yield();
            }

            Assert.True(exception == null);

            for (var j = 0L; j < operations; ++j)
            {
                var flag = cd.TryGetValue(j, out long val);

                Assert.True(flag && val == j);
            }
        }
    }
}
