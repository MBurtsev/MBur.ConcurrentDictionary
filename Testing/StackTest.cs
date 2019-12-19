using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nim.Collections.LockFree;
using System;
using System.Collections.Generic;

namespace Testing
{
    [TestClass]
    public class StackTest
    {
        const int operationsCount = 10_000_000;
        const int threadsCount = 8;

        SegmentStack<int> data;

        [TestInitialize]
        public void Init()
        {
            data = new SegmentStack<int>();
        }

        [TestCleanup]
        public void Clean()
        {
            data = null;
        }

        [TestMethod]
        public void GeneralTest()
        {
            var test = new ThreadTestHelper();

            // Generate data in some threads
            test.AddWorks(LockFreePushWork, threadsCount);
            test.Prepare();
            test.Begin();

            // Checking data
            // Each thread adds numbers from 0 to N. The idea is to count
            // the amount of each number. It should be equal to the count 
            // of threads. For example, if we have 8 threads, then the number 
            // 10 should be added 8 times.
            // Also, this method checks the correctness of the enumerator.
            CheckData(threadsCount);

            // Remove items in some threads
            test.Clear();
            test.AddWorks(LockFreePopWork, threadsCount);
            test.Prepare();
            test.Begin();

            // Count must be 0
            Assert.AreEqual(data.Count, 0, "Check count after pop work");
        }
        
        // Push work
        public void LockFreePushWork()
        {
            for (var i = 0; i < operationsCount; ++i)
            {
                data.Push(i);
            }
        }

        // Pop work
        public void LockFreePopWork()
        {
            for (var i = 0; i < operationsCount; ++i)
            {
                data.TryPop(out _);
            }
        }

        // check data
        public void CheckData(int checkSum)
        {
            var ht    = new Dictionary<int, int>();
            var total = 0;
            var err   = 0;

            foreach (var itm in data)
            {
                if (ht.ContainsKey(itm))
                {
                    ht[itm]++;
                }
                else
                {
                    ht.Add(itm, 1);
                }

                total++;
            }

            var flag = true;

            foreach (var key in ht.Keys)
            {
                var val = ht[key];

                if (val != checkSum)
                {
                    flag = false;

                    err++;

                    if (err < 10)
                    {
                        Console.WriteLine($"Key: {key}, val: {val}");
                    }
                }
            }

            Assert.AreEqual(total, data.Count, "Check count after push work");
            Assert.IsTrue(flag, $"Checked: {total} records, errors: {err}");
        }
    }
}
