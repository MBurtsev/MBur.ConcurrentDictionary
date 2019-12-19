// Maksim Burtsev https://github.com/nim
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Benchmark.Helpers
{
    // A class that helps organize multithreaded tests.
    public class ThreadsBenchHelper
    {
        // List of methods for testing.
        List<Action> list;
        // A thread is created for each method.
        List<Thread> threads;
        // Number of threads ready for work
        int  ready;
        // Signal to start work
        bool begin;
        // Number of threads complated work
        int complate;
        
        public ThreadsBenchHelper()
        {
            list = new List<Action>();
            threads = new List<Thread>();
        }

        // Add method for benchmark
        public void AddWorks(Action action, int count)
        {
            for (var i = 0; i < count; ++i)
            {
                list.Add(action);
            }
        }

        // Creating threads and waiting for readiness.
        public void Prepare()
        {
            for (var i = 0; i < list.Count; ++i)
            {
                var itm = list[i];
                var thread = new Thread(() => DoWork(itm));

                threads.Add(thread);

                thread.Start(); 
            }

            // waiting until all threads are ready
            while (Volatile.Read(ref ready) < threads.Count)
            {
            }
        }

        // An important condition for accuracy is the simultaneous 
        // inclusion of all threads in the work.
        // Threads will be launched at the same time as possible.
        void DoWork(Action action)
        {
            Interlocked.Add(ref ready, 1);

            // wait for the start
            while (!Volatile.Read(ref begin))
            {
            }

            action();

            Interlocked.Add(ref complate, 1);
        }

        // Start benchmark
        public void Begin()
        {
            Volatile.Write(ref begin, true);

            while (Volatile.Read(ref complate) < threads.Count)
            {
                Thread.Yield();
            }

            //foreach (var itm in threads)
            //{
            //    if (itm.ThreadState == ThreadState.Running)
            //    {
            //        itm.Join();
            //    }
            //}
        }

        // Clear benchmark for reuse
        public void Clear()
        {
            ready = 0;
            complate = 0;
            begin = false;
            
            list.Clear();
            threads.Clear();
        }
    }
}
