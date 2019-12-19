// Maksim Burtsev https://github.com/nim
// Licensed under the MIT license.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;

namespace Benchmark.Helpers
{
    public class GenericBenchConfig: ManualConfig
    {
        public GenericBenchConfig()
        {
            Add(Job.LongRun
                .WithLaunchCount(1)
                .WithWarmupCount(5)
                .WithIterationCount(20)
                .WithInvocationCount(1)
                .WithUnrollFactor(1)
                );


            Add(StatisticColumn.Min);
            Add(StatisticColumn.Max);
            Add(StatisticColumn.OperationsPerSecond);

            Add(HtmlExporter.Default);
            Add(MarkdownExporter.GitHub);
            Add(PlainExporter.Default);

            Add(JitOptimizationsValidator.DontFailOnError);
        }
    }
}
