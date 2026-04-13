using BenchmarkDotNet.Running;

namespace core.benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromTypes(
        [
            typeof(TransitionSelectionBenchmarks),
            typeof(HotPathAllocationBenchmarks),
            typeof(ReverseLookupBenchmarks)
        ]).Run(args);
    }
}