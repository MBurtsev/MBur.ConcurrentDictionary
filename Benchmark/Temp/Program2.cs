using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Benchmark.ThreadsBench;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmark
{
    class Program2
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<SolidStackPushBench>();


            //for (var i = 0; i < 100; i++)
            //{
            //    var cls = new SolidStackPushBench();

            //    cls.Threads = 1;
            //    cls.SolidStackPushSetup();

            //    var sw = Stopwatch.GetTimestamp();

            //    cls.BenchBegin();                
            //    //cls.BenchWork();

            //    var ms = (Stopwatch.GetTimestamp() - sw) / (double)Stopwatch.Frequency;
            //    var ops = (1000d / ms) * Class1.operationsCount * cls.Threads;

            //    Console.WriteLine("Op/s: " + ops.ToString("N2"));
            //}


            BenchmarkRunner.Run<StackBench>();
            //BenchmarkRunner.Run<Class1>();

            // Test with BenchmarkDotNet
            //BenchmarkRunner.Run<Class2>();

            //var cls = new Class2();
            //var sw = 0L;
            //var ms = 0d;
            //var ops = 0d;

            // Test 1 ------------------------------------
            //cls.Threads = 1;
            //cls.PushSetup();

            //var sw = Stopwatch.GetTimestamp();

            //cls.PushTest();

            //var ms = (Stopwatch.GetTimestamp() - sw) / (Stopwatch.Frequency / 1000d);
            //var ops = (1000d / ms)  * Class2.operationsCount;

            //Console.WriteLine();
            //Console.WriteLine("Op/s: " + ops.ToString("N2"));

            //// Clear ------------------------------------
            //GC.Collect();
            //GC.WaitForFullGCComplete();

            // Test 2 ------------------------------------
            //cls.PushSetup_capacity();
            //sw = Stopwatch.GetTimestamp();

            //cls.PushTest_capacity();

            //ms = (Stopwatch.GetTimestamp() - sw) / (Stopwatch.Frequency / 1000d);
            //ops = (1000d / ms)  * Class2.operationsCount;

            //Console.WriteLine("Op/s: " + ops.ToString("N2"));

            // End 2 ------------------------------------
            Console.ReadKey();
        }
    }

    //[Config(typeof(StackConfig))]
    public class Md5VsSha256
    {
        private SHA256 sha256 = SHA256.Create();
        private MD5 md5 = MD5.Create();
        private byte[] data;

        //[Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)]
        //public int Threads { get; set; }

        [Params(1000, 10000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public byte[] Sha256() => sha256.ComputeHash(data);

        [Benchmark]
        public byte[] Md5() => md5.ComputeHash(data);
    }
}
