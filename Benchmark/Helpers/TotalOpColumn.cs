// Maksim Burtsev https://github.com/MBurtsev
// Licensed under the MIT license.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

// Additional columnn total op/s. Calculate as number of threads * Op/s";
namespace Benchmark.Helpers
{
    public class TotalOpColumn : IColumn
    {
        public string Id => nameof(TotalOpColumn);

        public string ColumnName => "Op/s total";

        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 0;
        public bool IsNumeric => false;
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "Total operations per second. Calculate as number of threads * Op/s";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            var cols = summary.GetColumns();
            var threads = 0;
            var ops = 0d;

            foreach (var col in cols)
            {
                if (col.Id == "ParamColumn.Threads")
                {
                    var res = col.GetValue(summary, benchmarkCase);

                    threads = int.Parse(res);
                }

                if (col.Id == "StatisticColumn.Op/s")
                {
                    var res = col.GetValue(summary, benchmarkCase);

                    ops = double.Parse(res);

                    if (threads != 0)
                    {
                        break;
                    }
                }
            }

            return (threads * ops).ToString("N2");
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
    }
}
