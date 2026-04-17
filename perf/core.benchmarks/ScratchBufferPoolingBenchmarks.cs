using BenchmarkDotNet.Attributes;
using petrinets2.core;

namespace core.benchmarks;

[MemoryDiagnoser]
public class ScratchBufferPoolingBenchmarks
{
    ScratchBufferPoolingPlanningScenario planningScenario = null!;
    ScratchBufferPoolingFireScenario fireScenario = null!;
    ScratchBufferPoolingContentionScenario contentionScenario = null!;

    [Params(64, 256)]
    public int TransitionCount { get; set; }

    [Params(4, 8)]
    public int WorkerCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        planningScenario = ScratchBufferPoolingBenchmarkScenarios.CreatePlanningScenario(TransitionCount);
        fireScenario = ScratchBufferPoolingBenchmarkScenarios.CreateFireScenario(TransitionCount);
        contentionScenario = ScratchBufferPoolingBenchmarkScenarios.CreateContentionScenario(TransitionCount, WorkerCount, 32);
    }

    [Benchmark]
    public int Graph_Planning_Baseline()
    {
        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: false);
        return planningScenario.Graph.CreateFiringPlan(planningScenario.Marking).Count;
    }

    [Benchmark]
    public int Graph_Planning_Pooled()
    {
        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true);
        return planningScenario.Graph.CreateFiringPlan(planningScenario.Marking).Count;
    }

    [Benchmark]
    public Marking Graph_Fire_Baseline()
    {
        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: false);
        return fireScenario.Graph.Fire(fireScenario.Marking);
    }

    [Benchmark]
    public Marking Graph_Fire_Pooled()
    {
        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true);
        return fireScenario.Graph.Fire(fireScenario.Marking);
    }

    [Benchmark]
    public bool ReplayDeterminism_Pooled()
    {
        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true);

        var first = Snapshot(fireScenario.Graph.Fire(fireScenario.Marking));
        var second = Snapshot(fireScenario.Graph.Fire(fireScenario.Marking));
        return first.SequenceEqual(second);
    }

    [Benchmark]
    public long ConcurrencyStress_OwnershipViolations()
    {
        ScratchBufferPooling.ResetDiagnostics();
        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true);

        Parallel.For(0, contentionScenario.WorkerCount, _ =>
        {
            for (var i = 0; i < contentionScenario.Iterations; i++)
            {
                contentionScenario.Graph.Fire(contentionScenario.Marking);
            }
        });

        var diagnostics = ScratchBufferPooling.GetDiagnosticsSnapshot();
        return diagnostics.ReleaseViolations + diagnostics.DoubleReturnViolations;
    }

    static int[] Snapshot(Marking marking)
    {
        var snapshot = new int[marking.Size];
        for (var i = 0; i < marking.Size; i++)
        {
            snapshot[i] = marking[i];
        }

        return snapshot;
    }
}
