// Maksim Burtsev https://github.com/MBurtsev
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

        // A list of methods that are called by each thread. One for each.
        public List<Action> Actions => list;

        // Work will be restarted as long as this flag is enabled.
        public bool ReWork { get; set; }

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
                var index = i;
                var thread = new Thread(() => DoWork(index));

                threads.Add(thread);

                thread.Start(); 
            }

            WaitReady();
        }

        // waiting until all threads are ready
        public void WaitReady()
        {
            while (Volatile.Read(ref ready) < threads.Count)
            {
            }
        }

        // An important condition for accuracy is the simultaneous 
        // inclusion of all threads in the work.
        // Threads will be launched at the same time as possible.
        void DoWork(int index)
        {
            var action = list[index];

            Interlocked.Add(ref ready, 1);

            // wait for the start
            while (!Volatile.Read(ref begin))
            {
            }

            action();

            Interlocked.Add(ref complate, 1);

            // Restart work again
            if (ReWork)
            {
                // Restart will be after calling the Clear method.
                // If necessary it is permissible to replace the Actions with another.
                while (Volatile.Read(ref begin))
                {
                    Thread.Yield();
                }

                DoWork(index);
            }
        }

        // Start benchmark
        public void Begin()
        {
            Volatile.Write(ref begin, true);

            while (Volatile.Read(ref complate) < threads.Count)
            {
                Thread.Yield();
            }
        }

        // Clear benchmark for reuse
        public void Clear(bool onlyFlags = false)
        {
            ready = 0;
            complate = 0;
            begin = false;

            if (!onlyFlags)
            {
                list.Clear();
                threads.Clear();
            }
        }
    }
}
