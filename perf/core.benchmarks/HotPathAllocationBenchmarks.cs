using BenchmarkDotNet.Attributes;
using petrinets2.core;

namespace core.benchmarks;

[MemoryDiagnoser]
public class HotPathAllocationBenchmarks
{
    HotPathAllocationScenario conflictScenario = null!;
    HotPathAllocationScenario fireScenario = null!;

    [Params(32, 128)]
    public int BranchCount { get; set; }

    [Params(64, 256)]
    public int TransitionCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        conflictScenario = HotPathAllocationBenchmarkScenarios.CreateConflictScenario(BranchCount);
        fireScenario = HotPathAllocationBenchmarkScenarios.CreateFireScenario(TransitionCount);
    }

    [Benchmark]
    public bool Graph_PlaceIsConflicted()
    {
        return conflictScenario.Graph.PlaceIsConflicted(conflictScenario.ConflictPlaceId, conflictScenario.Marking);
    }

    [Benchmark]
    public bool Matrix_PlaceIsConflicted()
    {
        return conflictScenario.Matrix.PlaceIsConflicted(conflictScenario.ConflictPlaceId, conflictScenario.Marking);
    }

    [Benchmark]
    public Marking Graph_Fire()
    {
        return fireScenario.Graph.Fire(fireScenario.Marking);
    }

    [Benchmark]
    public Marking Matrix_Fire()
    {
        return fireScenario.Matrix.Fire(fireScenario.Marking);
    }
}
