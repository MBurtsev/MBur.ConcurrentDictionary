//#define LockFree_v1
#define LockFree_v2
//#define Concurrent

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#if LockFree_v1

using MBur.Collections.LockFree_v1;

#endif

#if LockFree_v2

using MBur.Collections.LockFree;

#endif

#if Concurrent

using System.Collections.Concurrent;

#endif

namespace XUnitTest
{
    public class Functional
    {
        private const int OPERATIONS = 1_000_000;
        // This number is necessary to calculate the base key.
        // It should be more than the number of operations.
        private const int THREADS_KEY_RANGE = 50_000_000;

        // Abbreviation for the names of the tested functions.

        // A    = ContainsKey(TKey key)
        // B    = TryGetValue(TKey key, out TValue value)
        // C    = TryAdd(TKey key, TValue value)
        // D    = TryRemove(KeyValuePair<TKey, TValue> item)
        // E    = TryRemove(TKey key, out TValue value)
        // F    = TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        // G    = GetOrAdd(TKey key, TValue value)
        // H    = GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        // I    = GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        // J    = AddOrUpdate<TArg>(TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
        // K    = AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        // L    = AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        // M    = AddOrUpdate(TKey key, TValue addValue, TValue updateValue)

        #region ' A. ContainsKey '

        #endregion

        #region ' B. TryGetValue '

        #endregion

        #region ' C. TryAdd '

        [Fact]
        public static void TryAdd_C1()
        {
            TryAdd_C1_Test(1);
            TryAdd_C1_Test(2);
            TryAdd_C1_Test(4);
        }
        static void TryAdd_C1_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.TryAdd(j, new KeyValuePair<long, long>(j, j));
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

            while (Volatile.Read(ref ready) < threads)
            {
                Thread.Yield();
            }

            Assert.True(exception == null);
            Assert.True(cd.Count == OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag && item.Key == j && item.Value == j);
            }
        }

        [Fact]
        public static void TryAdd_C2()
        {
            TryAdd_C2_Test(1);
            TryAdd_C2_Test(2);
            TryAdd_C2_Test(4);
        }
        static void TryAdd_C2_Test(int threads)
        {
            var ready = 0;
            var thread_id = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var key = Interlocked.Add(ref thread_id, 1) * THREADS_KEY_RANGE;

                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.TryAdd(key + j, new KeyValuePair<long, long>(j, j));
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

            while (Volatile.Read(ref ready) < threads)
            {
                Thread.Yield();
            }

            Assert.True(exception == null);
            Assert.True(cd.Count == OPERATIONS * threads);

            var dict = new Dictionary<long, int>();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;

                    Assert.True(cd.ContainsKey(key));

                    if (dict.ContainsKey(j))
                    {
                        dict[j]++;
                    }
                    else
                    {
                        dict.Add(j, 1);
                    }

                    var flag = cd.TryGetValue(key, out KeyValuePair<long, long> item);

                    Assert.True(flag);
                    Assert.Equal(item.Key, j);
                    Assert.Equal(item.Value, j);
                }
            }

            foreach (var itm in dict)
            {
                Assert.Equal(itm.Value, threads);
            }
        }

        #endregion

        #region ' D. TryRemove '

        #endregion

        #region ' E. TryRemove '

        #endregion

        #region ' F. TryUpdate '

        // Testing basic functionality
        [Fact]
        public static void TryUpdate_F1()
        {
            TryUpdate_F1_Test(1);
            TryUpdate_F1_Test(2);
            TryUpdate_F1_Test(4);
        }
        static void TryUpdate_F1_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                cd.TryAdd(j, new KeyValuePair<long, long>(j, j));
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.TryUpdate(j, new KeyValuePair<long, long>(-j, -j), new KeyValuePair<long, long>(j, j));
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

            while (Volatile.Read(ref ready) < threads)
            {
                Thread.Yield();
            }

            Assert.True(exception == null);
            Assert.True(cd.Count == OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag && item.Key == -j && item.Value == -j);
            }
        }

        // Testing the integrity of data acquisition in a highly competitive environment
        [Fact]
        public static void TryUpdate_F2()
        {
            TryUpdate_F2_Test(1);
            TryUpdate_F2_Test(2);
            TryUpdate_F2_Test(4);
        }
        static void TryUpdate_F2_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            cd.TryAdd(0, new KeyValuePair<long, long>(-1, -1));

            for (var i = 0; i < threads; ++i)
            {
                // writer
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.TryGetValue(0, out KeyValuePair<long, long> item);

                            if (item.Key != item.Value)
                            {
                                throw new Exception("item.Key != item.Value");
                            }

                            cd.TryUpdate(0, new KeyValuePair<long, long>(-j, -j), item);
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

            while (Volatile.Read(ref ready) < threads)
            {
                Thread.Yield();
            }

            Assert.True(exception == null);
            Assert.True(cd.Count == 1);
        }

        #endregion

        #region ' G. GetOrAdd '

        #endregion

        #region ' H. GetOrAdd '

        #endregion

        #region ' I. GetOrAdd '

        #endregion

        #region ' J. AddOrUpdate '

        #endregion

        #region ' K. AddOrUpdate '

        #endregion

        #region ' L. AddOrUpdate '

        #endregion

        #region ' M. AddOrUpdate '

#if LockFree_v1 || LockFree_v2

        // Test AddOrUpdate first Add after Updates
        [Fact]
        public static void AddOrUpdate_M1()
        {
            // Threads must be more than one!
            AddOrUpdate_M1_Test(2);
            AddOrUpdate_M1_Test(4);
        }
        static void AddOrUpdate_M1_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.AddOrUpdate(j, new KeyValuePair<long, long>(j, j), new KeyValuePair<long, long>(-j, -j));
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

            while (Volatile.Read(ref ready) < threads)
            {
                Thread.Yield();
            }

            Assert.True(exception == null);
            Assert.True(cd.Count == OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag && item.Key == -j && item.Value == -j);
            }
        }
#endif
        #endregion
    }
}
