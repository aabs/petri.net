using BenchmarkDotNet.Attributes;

namespace core.benchmarks;

[MemoryDiagnoser]
public class TransitionSelectionBenchmarks
{
    TransitionSelectionScenario scenario = null!;

    [Params(128, 512)]
    public int TransitionCount { get; set; }

    [Params(PriorityDistribution.Flat, PriorityDistribution.Mixed, PriorityDistribution.Skewed)]
    public PriorityDistribution Distribution { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        scenario = TransitionSelectionBenchmarkScenarios.Create(TransitionCount, Distribution);
    }

    [Benchmark]
    public int? Graph_GetNextTransitionToFire()
    {
        return scenario.Graph.GetNextTransitionToFire(scenario.Marking);
    }

    [Benchmark]
    public int? Matrix_GetNextTransitionToFire()
    {
        return scenario.Matrix.GetNextTransitionToFire(scenario.Marking);
    }
}