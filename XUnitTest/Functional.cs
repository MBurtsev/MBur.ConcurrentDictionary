#define LockFree_v1
//#define LockFree_v2
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

        [Fact]
        public static void ContainsKey_A1()
        {
            ContainsKey_A1_Test(1);
            ContainsKey_A1_Test(2);
            ContainsKey_A1_Test(4);
        }
        static void ContainsKey_A1_Test(int threads)
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
            Assert.Equal(cd.Count, OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                Assert.True(cd.ContainsKey(j));
            }
        }


        [Fact]
        public static void ContainsKey_A2()
        {
            ContainsKey_A2_Test(1);
            ContainsKey_A2_Test(2);
            ContainsKey_A2_Test(4);
        }
        static void ContainsKey_A2_Test(int threads)
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
            Assert.Equal(cd.Count, OPERATIONS * threads);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;

                    Assert.True(cd.ContainsKey(key));
                }
            }
        }

        #endregion

        #region ' B. TryGetValue '

        [Fact]
        public static void TryGetValue_B1()
        {
            TryGetValue_B1_Test(1);
            TryGetValue_B1_Test(2);
            TryGetValue_B1_Test(4);
        }
        static void TryGetValue_B1_Test(int threads)
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
            Assert.Equal(cd.Count, OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, j);
                Assert.Equal(item.Value, j);
            }
        }

        [Fact]
        public static void TryGetValue_B2()
        {
            TryGetValue_B2_Test(1);
            TryGetValue_B2_Test(2);
            TryGetValue_B2_Test(4);
        }
        static void TryGetValue_B2_Test(int threads)
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
            Assert.Equal(cd.Count, OPERATIONS * threads);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;
                    var flag = cd.TryGetValue(key, out KeyValuePair<long, long> item);

                    Assert.True(flag);
                    Assert.Equal(item.Key, j);
                    Assert.Equal(item.Value, j);
                }
            }
        }

        // Integrity reading while recording by other threads.
        [Fact]
        public static void TryGetValue_B3()
        {
            TryGetValue_B3_Test(1);
            TryGetValue_B3_Test(2);
            TryGetValue_B3_Test(4);
        }
        static void TryGetValue_B3_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            for (var i = 0; i < threads; ++i)
            {
                // writer
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd[0] = new KeyValuePair<long, long>(j, j);
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

                // reader
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            if (cd.TryGetValue(0, out KeyValuePair<long, long> item))
                            {
                                if (item.Key != item.Value)
                                {
                                    throw new Exception($"{item.Key} != {item.Value}");
                                }
                            }
                        }
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

            cts.Cancel();

            Assert.True(exception == null);
            Assert.Single(cd);
        }

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
            Assert.Equal(cd.Count, OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, j);
                Assert.Equal(item.Value, j);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.Equal(item.Key, item.Value.Key);
                Assert.Equal(item.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS);
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
            Assert.Equal(cd.Count, OPERATIONS * threads);

            var dict = new Dictionary<long, int>();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;
                    var flag = cd.TryGetValue(key, out KeyValuePair<long, long> item);

                    Assert.True(flag);
                    Assert.Equal(item.Key, j);
                    Assert.Equal(item.Value, j);

                    Assert.True(cd.ContainsKey(key));

                    if (dict.ContainsKey(item.Key))
                    {
                        dict[item.Key]++;
                    }
                    else
                    {
                        dict.Add(item.Key, 1);
                    }
                }
            }

            foreach (var item in dict)
            {
                Assert.Equal(item.Value, threads);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.True(dict.ContainsKey(item.Value.Key));
                Assert.Equal(item.Value.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS * threads);
        }

        #endregion

        #region ' D. TryRemove '

        [Fact]
        public static void TryRemove_D1()
        {
            TryRemove_D1_Test(1);
            TryRemove_D1_Test(2);
            TryRemove_D1_Test(4);
        }
        static void TryRemove_D1_Test(int threads)
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
                            if (cd.TryRemove(j, out KeyValuePair<long, long> item))
                            {
                                if (item.Key != item.Value)
                                {
                                    throw new Exception($"{item.Key} != {item.Value}");
                                }
                            }
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
            Assert.Empty(cd);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                Assert.False(cd.ContainsKey(j));
            }

            foreach (var item in cd)
            {
                throw new Exception("Must be nothing");
            }
        }

        [Fact]
        public static void TryRemove_D2()
        {
            TryRemove_D2_Test(1);
            TryRemove_D2_Test(2);
            TryRemove_D2_Test(4);
        }
        static void TryRemove_D2_Test(int threads)
        {
            var ready = 0;
            var thread_id = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;

                    cd.TryAdd(key, new KeyValuePair<long, long>(j, j));
                }
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var key = Interlocked.Add(ref thread_id, 1) * THREADS_KEY_RANGE;

                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            if (cd.TryRemove(key + j, out KeyValuePair<long, long> item))
                            {
                                if (item.Key != item.Value)
                                {
                                    throw new Exception($"{item.Key} != {item.Value}");
                                }
                            }
                            else
                            {
                                throw new Exception($"Item key={key} not exist");
                            }
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
            Assert.Empty(cd);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;

                    Assert.False(cd.ContainsKey(key));
                }
            }

            foreach (var item in cd)
            {
                throw new Exception("Must be nothing");
            }
        }

        #endregion

        #region ' E. TryRemove '

        // This overload of TryRemove included in .net5
#if LockFree_v1 || LockFree_v2

        [Fact]
        public static void TryRemove_E1()
        {
            TryRemove_E1_Test(1);
            TryRemove_E1_Test(2);
            TryRemove_E1_Test(4);
        }
        static void TryRemove_E1_Test(int threads)
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
                            var item = new KeyValuePair<long,KeyValuePair<long, long>>(j, new KeyValuePair<long, long>(j ,j));

                            cd.TryRemove(item);
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
            Assert.Empty(cd);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                Assert.False(cd.ContainsKey(j));
            }

            foreach (var item in cd)
            {
                throw new Exception("Must be nothing");
            }
        }

        [Fact]
        public static void TryRemove_E2()
        {
            TryRemove_E2_Test(1);
            TryRemove_E2_Test(2);
            TryRemove_E2_Test(4);
        }
        static void TryRemove_E2_Test(int threads)
        {
            var ready = 0;
            var thread_id = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;

                    cd.TryAdd(key, new KeyValuePair<long, long>(j, j));
                }
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var key = Interlocked.Add(ref thread_id, 1) * THREADS_KEY_RANGE;

                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            var item = new KeyValuePair<long, KeyValuePair<long, long>>(key + j, new KeyValuePair<long, long>(j, j));

                            if (!cd.TryRemove(item))
                            {
                                throw new Exception($"Item key={key} not exist");
                            }
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
            Assert.Empty(cd);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;

                    Assert.False(cd.ContainsKey(key));
                }
            }

            foreach (var item in cd)
            {
                throw new Exception("Must be nothing");
            }
        }
#endif
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
            Assert.Equal(cd.Count, OPERATIONS);

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
            Assert.Equal(cd.Count, 1);
        }

        // Update and enumeration
        [Fact]
        public static void TryUpdate_F3()
        {
            TryUpdate_F3_Test(1);
            TryUpdate_F3_Test(2);
            TryUpdate_F3_Test(4);
        }
        static void TryUpdate_F3_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            for (var i = 0; i < threads; ++i)
            {
                // writer
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd[0] = new KeyValuePair<long, long>(j, j);
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

                // reader
                Task.Run(() =>
                {
                    try
                    {
                        while(true)
                        {
                            foreach (var item in cd)
                            {
                                if (item.Value.Key != item.Value.Value)
                                {
                                    throw new Exception($"{item.Key} != {item.Value}");
                                }
                            }
                        }
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

            cts.Cancel();

            Assert.True(exception == null);
            Assert.Single(cd);
        }

        #endregion

        #region ' G. GetOrAdd '

        // add mode
        [Fact]
        public static void GetOrAdd_G1()
        {
            GetOrAdd_G1_Test(1);
            GetOrAdd_G1_Test(2);
            GetOrAdd_G1_Test(4);
        }
        static void GetOrAdd_G1_Test(int threads)
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
                            cd.GetOrAdd(j, new KeyValuePair<long, long>(j, j));
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
            Assert.Equal(cd.Count, OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, j);
                Assert.Equal(item.Value, j);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.Equal(item.Key, item.Value.Key);
                Assert.Equal(item.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS);
        }

        // add mode
        [Fact]
        public static void GetOrAdd_G2()
        {
            GetOrAdd_G2_Test(1);
            GetOrAdd_G2_Test(2);
            GetOrAdd_G2_Test(4);
        }
        static void GetOrAdd_G2_Test(int threads)
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
                            cd.GetOrAdd(key + j, new KeyValuePair<long, long>(j, j));
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
            Assert.Equal(cd.Count, OPERATIONS * threads);

            var dict = new Dictionary<long, int>();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;
                    var flag = cd.TryGetValue(key, out KeyValuePair<long, long> item);

                    Assert.True(flag);
                    Assert.Equal(item.Key, j);
                    Assert.Equal(item.Value, j);

                    Assert.True(cd.ContainsKey(key));

                    if (dict.ContainsKey(item.Key))
                    {
                        dict[item.Key]++;
                    }
                    else
                    {
                        dict.Add(item.Key, 1);
                    }
                }
            }

            foreach (var item in dict)
            {
                Assert.Equal(item.Value, threads);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.True(dict.ContainsKey(item.Value.Key));
                Assert.Equal(item.Value.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS * threads);
        }

        // get mode + update
        [Fact]
        public static void GetOrAdd_G3()
        {
            GetOrAdd_G3_Test(1);
            GetOrAdd_G3_Test(2);
            GetOrAdd_G3_Test(4);
        }
        static void GetOrAdd_G3_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            for (var i = 0; i < threads; ++i)
            {
                // writer
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd[0] = new KeyValuePair<long, long>(j, j);
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

                // reader
                Task.Run(() =>
                {
                    var tmp = new KeyValuePair<long, long>(-1, -1);

                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            var item = cd.GetOrAdd(0, tmp);

                            if (item.Key != item.Value)
                            {
                                throw new Exception($"{item.Key} != {item.Value}");
                            }
                        }
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

            cts.Cancel();

            Assert.True(exception == null);
            Assert.Single(cd);
        }

        #endregion

        #region ' H. GetOrAdd '

        // add mode
        [Fact]
        public static void GetOrAdd_H1()
        {
            GetOrAdd_H1_Test(1);
            GetOrAdd_H1_Test(2);
            GetOrAdd_H1_Test(4);
        }
        static void GetOrAdd_H1_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();
            var requested = 0;

            KeyValuePair<long, long> valueFactory(long key)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key, key);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.GetOrAdd(j, valueFactory);
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
            Assert.Equal(cd.Count, OPERATIONS);

#if LockFree_v1 || LockFree_v2
            // The LockFree hash table calls valueFactory only if it is really needed 
            // and the value will be used to insert or update. The current version may 
            // call valueFactory even if the value will not be used, which is a drawback.
            
            Assert.Equal(requested, OPERATIONS);
#endif

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, j);
                Assert.Equal(item.Value, j);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.Equal(item.Key, item.Value.Key);
                Assert.Equal(item.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS);
        }

        // add mode
        [Fact]
        public static void GetOrAdd_H2()
        {
            GetOrAdd_H2_Test(1);
            GetOrAdd_H2_Test(2);
            GetOrAdd_H2_Test(4);
        }
        static void GetOrAdd_H2_Test(int threads)
        {
            var ready = 0;
            var thread_id = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();
            var requested = 0;

            KeyValuePair<long, long> valueFactory(long key)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key % THREADS_KEY_RANGE, key % THREADS_KEY_RANGE);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var key = Interlocked.Add(ref thread_id, 1) * THREADS_KEY_RANGE;

                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.GetOrAdd(key + j, valueFactory);
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
            Assert.Equal(cd.Count, OPERATIONS * threads);
            Assert.Equal(requested, OPERATIONS * threads);

            var dict = new Dictionary<long, int>();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;
                    var flag = cd.TryGetValue(key, out KeyValuePair<long, long> item);

                    Assert.True(flag);
                    Assert.Equal(item.Key, j);
                    Assert.Equal(item.Value, j);

                    Assert.True(cd.ContainsKey(key));

                    if (dict.ContainsKey(item.Key))
                    {
                        dict[item.Key]++;
                    }
                    else
                    {
                        dict.Add(item.Key, 1);
                    }
                }
            }

            foreach (var item in dict)
            {
                Assert.Equal(item.Value, threads);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.True(dict.ContainsKey(item.Value.Key));
                Assert.Equal(item.Value.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS * threads);
        }

        // get mode + update
        [Fact]
        public static void GetOrAdd_H3()
        {
            GetOrAdd_H3_Test(1);
            GetOrAdd_H3_Test(2);
            GetOrAdd_H3_Test(4);
        }
        static void GetOrAdd_H3_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            KeyValuePair<long, long> valueFactory(long key)
            {
                return new KeyValuePair<long, long>(key % THREADS_KEY_RANGE, key % THREADS_KEY_RANGE);
            }

            for (var i = 0; i < threads; ++i)
            {
                // writer
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd[0] = new KeyValuePair<long, long>(j, j);
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

                // reader
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            var item = cd.GetOrAdd(0, valueFactory);

                            if (item.Key != item.Value)
                            {
                                throw new Exception($"{item.Key} != {item.Value}");
                            }
                        }
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

            cts.Cancel();

            Assert.True(exception == null);
            Assert.Single(cd);
        }

        #endregion

        #region ' I. GetOrAdd '

        // add mode
        [Fact]
        public static void GetOrAdd_I1()
        {
            GetOrAdd_I1_Test(1);
            GetOrAdd_I1_Test(2);
            GetOrAdd_I1_Test(4);
        }
        static void GetOrAdd_I1_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();
            var requested = 0;

            KeyValuePair<long, long> valueFactory(long key, long arg)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(arg, arg);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.GetOrAdd(j, valueFactory, j);
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
            Assert.Equal(cd.Count, OPERATIONS);

#if LockFree_v1 || LockFree_v2
            // The LockFree hash table calls valueFactory only if it is really needed 
            // and the value will be used to insert or update. The current version may 
            // call valueFactory even if the value will not be used, which is a drawback.
            
            Assert.Equal(requested, OPERATIONS);
#endif

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, j);
                Assert.Equal(item.Value, j);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.Equal(item.Key, item.Value.Key);
                Assert.Equal(item.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS);
        }

        // add mode
        [Fact]
        public static void GetOrAdd_I2()
        {
            GetOrAdd_I2_Test(1);
            GetOrAdd_I2_Test(2);
            GetOrAdd_I2_Test(4);
        }
        static void GetOrAdd_I2_Test(int threads)
        {
            var ready = 0;
            var thread_id = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();
            var requested = 0;

            KeyValuePair<long, long> valueFactory(long key, long arg)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(arg, arg);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var key = Interlocked.Add(ref thread_id, 1) * THREADS_KEY_RANGE;

                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.GetOrAdd(key + j, valueFactory, j);
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
            Assert.Equal(cd.Count, OPERATIONS * threads);
            Assert.Equal(requested, OPERATIONS * threads);

            var dict = new Dictionary<long, int>();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;
                    var flag = cd.TryGetValue(key, out KeyValuePair<long, long> item);

                    Assert.True(flag);
                    Assert.Equal(item.Key, j);
                    Assert.Equal(item.Value, j);

                    Assert.True(cd.ContainsKey(key));

                    if (dict.ContainsKey(item.Key))
                    {
                        dict[item.Key]++;
                    }
                    else
                    {
                        dict.Add(item.Key, 1);
                    }
                }
            }

            foreach (var item in dict)
            {
                Assert.Equal(item.Value, threads);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.True(dict.ContainsKey(item.Value.Key));
                Assert.Equal(item.Value.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS * threads);
        }

        // get mode + update
        [Fact]
        public static void GetOrAdd_I3()
        {
            GetOrAdd_I3_Test(1);
            GetOrAdd_I3_Test(2);
            GetOrAdd_I3_Test(4);
        }
        static void GetOrAdd_I3_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            KeyValuePair<long, long> valueFactory(long key, long arg)
            {
                return new KeyValuePair<long, long>(arg, arg);
            }

            for (var i = 0; i < threads; ++i)
            {
                // writer
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd[0] = new KeyValuePair<long, long>(j, j);
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

                // reader
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            var item = cd.GetOrAdd(0, valueFactory, j);

                            if (item.Key != item.Value)
                            {
                                throw new Exception($"{item.Key} != {item.Value}");
                            }
                        }
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

            cts.Cancel();

            Assert.True(exception == null);
            Assert.Single(cd);
        }

#endregion

        #region ' J. AddOrUpdate '

        // add mode
        [Fact]
        public static void AddOrUpdate_J1()
        {
            AddOrUpdate_J1_Test(1);
            AddOrUpdate_J1_Test(2);
            AddOrUpdate_J1_Test(4);
        }
        static void AddOrUpdate_J1_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();
            var requested = 0;

            KeyValuePair<long, long> addValueFactory(long key, long arg)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key % THREADS_KEY_RANGE, key % THREADS_KEY_RANGE);
            }

            KeyValuePair<long, long> updateValueFactory(long key, KeyValuePair<long, long> current, long arg)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key % THREADS_KEY_RANGE, key % THREADS_KEY_RANGE);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.AddOrUpdate(j, addValueFactory, updateValueFactory, 0L);
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
            Assert.Equal(cd.Count, OPERATIONS);

#if LockFree_v1 || LockFree_v2
            // The LockFree hash table calls valueFactory only if it is really needed 
            // and the value will be used to insert or update. The current version may 
            // call valueFactory even if the value will not be used, which is a drawback.
            
            Assert.Equal(requested, OPERATIONS * threads);
#endif

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, j);
                Assert.Equal(item.Value, j);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.Equal(item.Key, item.Value.Key);
                Assert.Equal(item.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS);
        }

        // add mode
        [Fact]
        public static void AddOrUpdate_J2()
        {
            AddOrUpdate_J2_Test(1);
            AddOrUpdate_J2_Test(2);
            AddOrUpdate_J2_Test(4);
        }
        static void AddOrUpdate_J2_Test(int threads)
        {
            var ready = 0;
            var thread_id = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();
            var requested = 0;

            KeyValuePair<long, long> addValueFactory(long key, long arg)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key % THREADS_KEY_RANGE, key % THREADS_KEY_RANGE);
            }

            KeyValuePair<long, long> updateValueFactory(long key, KeyValuePair<long, long> current, long arg)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key % THREADS_KEY_RANGE, key % THREADS_KEY_RANGE);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var key = Interlocked.Add(ref thread_id, 1) * THREADS_KEY_RANGE;

                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.AddOrUpdate(key + j, addValueFactory, updateValueFactory, 0L);
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
            Assert.Equal(cd.Count, OPERATIONS * threads);
            Assert.Equal(requested, OPERATIONS * threads);

            var dict = new Dictionary<long, int>();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;
                    var flag = cd.TryGetValue(key, out KeyValuePair<long, long> item);

                    Assert.True(flag);
                    Assert.Equal(item.Key, j);
                    Assert.Equal(item.Value, j);

                    Assert.True(cd.ContainsKey(key));

                    if (dict.ContainsKey(item.Key))
                    {
                        dict[item.Key]++;
                    }
                    else
                    {
                        dict.Add(item.Key, 1);
                    }
                }
            }

            foreach (var item in dict)
            {
                Assert.Equal(item.Value, threads);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.True(dict.ContainsKey(item.Value.Key));
                Assert.Equal(item.Value.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS * threads);
        }

        // First Add after Updates
        [Fact]
        public static void AddOrUpdate_J3()
        {
            // Threads must be more than one!
            AddOrUpdate_J3_Test(2);
            AddOrUpdate_J3_Test(4);
        }
        static void AddOrUpdate_J3_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            KeyValuePair<long, long> addValueFactory(long key, long arg)
            {
                return new KeyValuePair<long, long>(key % THREADS_KEY_RANGE, key % THREADS_KEY_RANGE);
            }

            KeyValuePair<long, long> updateValueFactory(long key, KeyValuePair<long, long> current, long arg)
            {
                return new KeyValuePair<long, long>(-key % THREADS_KEY_RANGE, -key % THREADS_KEY_RANGE);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.AddOrUpdate(j, addValueFactory, updateValueFactory, 0L);
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
            Assert.Equal(cd.Count, OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, -j);
                Assert.Equal(item.Value, -j);
            }
        }

#endregion

        #region ' K. AddOrUpdate '

        // add mode
        [Fact]
        public static void AddOrUpdate_K1()
        {
            AddOrUpdate_K1_Test(1);
            AddOrUpdate_K1_Test(2);
            AddOrUpdate_K1_Test(4);
        }
        static void AddOrUpdate_K1_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();
            var requested = 0;

            KeyValuePair<long, long> addValueFactory(long key)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key, key);
            }

            KeyValuePair<long, long> updateValueFactory(long key, KeyValuePair<long, long> current)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key, key);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.AddOrUpdate(j, addValueFactory, updateValueFactory);
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
            Assert.Equal(cd.Count, OPERATIONS);
            
#if LockFree_v1 || LockFree_v2
            // The LockFree hash table calls valueFactory only if it is really needed 
            // and the value will be used to insert or update. The current version may 
            // call valueFactory even if the value will not be used, which is a drawback.
            
            Assert.Equal(requested, OPERATIONS * threads);
#endif

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, j);
                Assert.Equal(item.Value, j);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.Equal(item.Key, item.Value.Key);
                Assert.Equal(item.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS);
        }

        // add mode
        [Fact]
        public static void AddOrUpdate_K2()
        {
            AddOrUpdate_K2_Test(1);
            AddOrUpdate_K2_Test(2);
            AddOrUpdate_K2_Test(4);
        }
        static void AddOrUpdate_K2_Test(int threads)
        {
            var ready = 0;
            var thread_id = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();
            var requested = 0;

            KeyValuePair<long, long> addValueFactory(long key)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key % THREADS_KEY_RANGE, key % THREADS_KEY_RANGE);
            }

            KeyValuePair<long, long> updateValueFactory(long key, KeyValuePair<long, long> current)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key % THREADS_KEY_RANGE, key % THREADS_KEY_RANGE);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var key = Interlocked.Add(ref thread_id, 1) * THREADS_KEY_RANGE;

                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.AddOrUpdate(key + j, addValueFactory, updateValueFactory);
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
            Assert.Equal(cd.Count, OPERATIONS * threads);
            Assert.Equal(requested, OPERATIONS * threads);

            var dict = new Dictionary<long, int>();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;
                    var flag = cd.TryGetValue(key, out KeyValuePair<long, long> item);

                    Assert.True(flag);
                    Assert.Equal(item.Key, j);
                    Assert.Equal(item.Value, j);

                    Assert.True(cd.ContainsKey(key));

                    if (dict.ContainsKey(item.Key))
                    {
                        dict[item.Key]++;
                    }
                    else
                    {
                        dict.Add(item.Key, 1);
                    }
                }
            }

            foreach (var item in dict)
            {
                Assert.Equal(item.Value, threads);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.True(dict.ContainsKey(item.Value.Key));
                Assert.Equal(item.Value.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS * threads);
        }

        // First Add after Updates
        [Fact]
        public static void AddOrUpdate_K3()
        {
            // Threads must be more than one!
            AddOrUpdate_K3_Test(2);
            AddOrUpdate_K3_Test(4);
        }
        static void AddOrUpdate_K3_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            KeyValuePair<long, long> addValueFactory(long key)
            {
                return new KeyValuePair<long, long>(key, key);
            }

            KeyValuePair<long, long> updateValueFactory(long key, KeyValuePair<long, long> current)
            {
                return new KeyValuePair<long, long>(-key % THREADS_KEY_RANGE, -key % THREADS_KEY_RANGE);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.AddOrUpdate(j, addValueFactory, updateValueFactory);
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
            Assert.Equal(cd.Count, OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, -j);
                Assert.Equal(item.Value, -j);
            }
        }

#endregion

        #region ' L. AddOrUpdate '

        // add mode
        [Fact]
        public static void AddOrUpdate_L1()
        {
            AddOrUpdate_L1_Test(1);
            AddOrUpdate_L1_Test(2);
            AddOrUpdate_L1_Test(4);
        }
        static void AddOrUpdate_L1_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();
            var requested = 0;

            KeyValuePair<long, long> updateValueFactory(long key, KeyValuePair<long, long> current)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key, key);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.AddOrUpdate(j, new KeyValuePair<long, long>(j, j), updateValueFactory);
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
            Assert.Equal(cd.Count, OPERATIONS);
            Assert.Equal(requested, OPERATIONS * (threads - 1));

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, j);
                Assert.Equal(item.Value, j);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.Equal(item.Key, item.Value.Key);
                Assert.Equal(item.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS);
        }

        // add mode
        [Fact]
        public static void AddOrUpdate_L2()
        {
            AddOrUpdate_L2_Test(1);
            AddOrUpdate_L2_Test(2);
            AddOrUpdate_L2_Test(4);
        }
        static void AddOrUpdate_L2_Test(int threads)
        {
            var ready = 0;
            var thread_id = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();
            var requested = 0;

            KeyValuePair<long, long> updateValueFactory(long key, KeyValuePair<long, long> current)
            {
                Interlocked.Add(ref requested, 1);

                return new KeyValuePair<long, long>(key, key);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var key = Interlocked.Add(ref thread_id, 1) * THREADS_KEY_RANGE;

                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.AddOrUpdate(key + j, new KeyValuePair<long, long>(j, j), updateValueFactory);
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
            Assert.Equal(cd.Count, OPERATIONS * threads);
            Assert.Equal(0, requested);

            var dict = new Dictionary<long, int>();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;
                    var flag = cd.TryGetValue(key, out KeyValuePair<long, long> item);

                    Assert.True(flag);
                    Assert.Equal(item.Key, j);
                    Assert.Equal(item.Value, j);

                    Assert.True(cd.ContainsKey(key));

                    if (dict.ContainsKey(item.Key))
                    {
                        dict[item.Key]++;
                    }
                    else
                    {
                        dict.Add(item.Key, 1);
                    }
                }
            }

            foreach (var item in dict)
            {
                Assert.Equal(item.Value, threads);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.True(dict.ContainsKey(item.Value.Key));
                Assert.Equal(item.Value.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS * threads);
        }

        // First Add after Updates
        [Fact]
        public static void AddOrUpdate_L3()
        {
            // Threads must be more than one!
            AddOrUpdate_L3_Test(2);
            AddOrUpdate_L3_Test(4);
        }
        static void AddOrUpdate_L3_Test(int threads)
        {
            var ready = 0;
            var exception = null as Exception;
            var cd = new ConcurrentDictionary<long, KeyValuePair<long, long>>();
            var cts = new CancellationTokenSource();

            KeyValuePair<long, long> updateValueFactory(long key, KeyValuePair<long, long> current)
            {
                return new KeyValuePair<long, long>(- key % THREADS_KEY_RANGE, - key % THREADS_KEY_RANGE);
            }

            for (var i = 0; i < threads; ++i)
            {
                Task.Run(() =>
                {
                    try
                    {
                        for (var j = 0L; j < OPERATIONS; ++j)
                        {
                            cd.AddOrUpdate(j, new KeyValuePair<long, long>(j, j), updateValueFactory);
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
            Assert.Equal(cd.Count, OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, -j);
                Assert.Equal(item.Value, -j);
            }
        }

#endregion

        #region ' M. AddOrUpdate '

#if LockFree_v1 || LockFree_v2

        // add mode
        [Fact]
        public static void AddOrUpdate_M1()
        {
            AddOrUpdate_M1_Test(1);
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
                            cd.AddOrUpdate(j, new KeyValuePair<long, long>(j, j), new KeyValuePair<long, long>(j, j));
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
            Assert.Equal(cd.Count, OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, j);
                Assert.Equal(item.Value, j);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.Equal(item.Key, item.Value.Key);
                Assert.Equal(item.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS);
        }

        // add mode
        [Fact]
        public static void AddOrUpdate_M2()
        {
            AddOrUpdate_M2_Test(1);
            AddOrUpdate_M2_Test(2);
            AddOrUpdate_M2_Test(4);
        }
        static void AddOrUpdate_M2_Test(int threads)
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
                            cd.AddOrUpdate(key + j, new KeyValuePair<long, long>(j, j), new KeyValuePair<long, long>(j, j));
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
            Assert.Equal(cd.Count, OPERATIONS * threads);

            var dict = new Dictionary<long, int>();

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                for (var t = 1; t <= threads; ++t)
                {
                    var key = j + t * THREADS_KEY_RANGE;
                    var flag = cd.TryGetValue(key, out KeyValuePair<long, long> item);

                    Assert.True(flag);
                    Assert.Equal(item.Key, j);
                    Assert.Equal(item.Value, j);

                    Assert.True(cd.ContainsKey(key));

                    if (dict.ContainsKey(item.Key))
                    {
                        dict[item.Key]++;
                    }
                    else
                    {
                        dict.Add(item.Key, 1);
                    }
                }
            }

            foreach (var item in dict)
            {
                Assert.Equal(item.Value, threads);
            }

            var count = 0;

            foreach (var item in cd)
            {
                count++;

                Assert.True(dict.ContainsKey(item.Value.Key));
                Assert.Equal(item.Value.Key, item.Value.Value);
            }

            Assert.Equal(count, OPERATIONS * threads);
        }

        // First Add after Updates
        [Fact]
        public static void AddOrUpdate_M3()
        {
            // Threads must be more than one!
            AddOrUpdate_M3_Test(2);
            AddOrUpdate_M3_Test(4);
        }
        static void AddOrUpdate_M3_Test(int threads)
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
            Assert.Equal(cd.Count, OPERATIONS);

            for (var j = 0L; j < OPERATIONS; ++j)
            {
                var flag = cd.TryGetValue(j, out KeyValuePair<long, long> item);

                Assert.True(flag);
                Assert.Equal(item.Key, -j);
                Assert.Equal(item.Value, -j);
            }
        }
#endif
#endregion
    }
}
