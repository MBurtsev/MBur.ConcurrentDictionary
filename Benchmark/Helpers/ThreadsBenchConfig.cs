// Maksim Burtsev https://github.com/MBurtsev
// Licensed under the MIT license.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;
using System.Globalization;

namespace Benchmark.Helpers
{
    // Settings for all multi-threaded tests
    public class ThreadsBenchConfig : ManualConfig
    {
        //The number of iterations of the test method. This number greatly affects the 
        //accuracy of the results. The bigger it is, the better.
        public const int OperationsCount = 10_000_000;

        // Number of threads
        public static int[] Threads = new[]
        {
            /**/01/*, 02 , 03, 04, 05, 06, 07, 08, 09, 10, 11, 12, 13, 14, 15, 16*/
        };

        public static CultureInfo Culture = new CultureInfo("en-US");

        public ThreadsBenchConfig()
        {
            Add(Job.MediumRun
                .WithLaunchCount(1)
                .WithWarmupCount(3) // 2
                .WithIterationCount(5) // 5
                .WithInvocationCount(1)
                .WithUnrollFactor(1)
                );


            Add(StatisticColumn.Min);
            Add(StatisticColumn.Max);
            Add(StatisticColumn.OperationsPerSecond);
            Add(new TotalOpColumn());

            Add(HtmlExporter.Default);
            Add(MarkdownExporter.GitHub);
            Add(PlainExporter.Default);

            Add(JitOptimizationsValidator.DontFailOnError);
        }
    }
}
