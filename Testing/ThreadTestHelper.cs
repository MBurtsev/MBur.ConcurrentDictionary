using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Testing
{
    public class ThreadTestHelper
    {
        List<Action> list;
        List<Thread> threads;
        int  ready;
        bool begin;


        public ThreadTestHelper()
        {
            list = new List<Action>();
            threads = new List<Thread>();
        }

        public void AddWorks(Action action, int count)
        {
            for (var i = 0; i < count; ++i)
            {
                list.Add(action);
            }
        }

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

        void DoWork(Action action)
        {
            Interlocked.Add(ref ready, 1);

            while (!Volatile.Read(ref begin))
            {
            }

            action();
        }

        public void Begin()
        {
            Volatile.Write(ref begin, true);

            for (var i = 0; i < threads.Count; ++i)
            {
                var itm = threads[i];

                if (itm.ThreadState == ThreadState.Running)
                {
                    itm.Join();
                }
            }
        }

        public void Clear()
        {
            list.Clear();
            threads.Clear();
        }
    }
}
