using BenchmarkDotNet.Attributes;
using petrinets2.core;

namespace core.benchmarks;

[MemoryDiagnoser]
public class SparseMatrixKernelBenchmarks
{
    SparseMatrixFireScenario fireScenario = null!;
    StateEquationScenario stateEquationScenario = null!;

    [Params(128, 512)]
    public int TransitionCount { get; set; }

    [Params(DensityBand.Low, DensityBand.Medium, DensityBand.High)]
    public DensityBand DensityBand { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        fireScenario = SparseMatrixBenchmarkScenarios.CreateFireScenario(TransitionCount, DensityBand);
        stateEquationScenario = StateEquationBenchmarkScenarios.Create(TransitionCount, DensityBand);
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

    [Benchmark]
    public int[] Graph_StateEquationDelta()
    {
        return ComputeStateEquationDeltaFromGraph(stateEquationScenario);
    }

    [Benchmark]
    public int[] Matrix_StateEquationDelta()
    {
        return StateEquationBenchmarkScenarios.ComputeStateEquationDelta(stateEquationScenario);
    }

    static int[] ComputeStateEquationDeltaFromGraph(StateEquationScenario scenario)
    {
        var deltas = new int[scenario.Marking.Size];
        foreach (var transitionId in scenario.TransitionIds)
        {
            if (scenario.Graph.OutArcs.TryGetValue(transitionId, out var outArcs))
            {
                foreach (var outArc in outArcs)
                {
                    deltas[outArc.Target] += outArc.Weight;
                }
            }

            if (!scenario.Graph.InArcs.TryGetValue(transitionId, out var inArcs))
            {
                continue;
            }

            foreach (var inArc in inArcs)
            {
                if (inArc.IsInhibitor)
                {
                    continue;
                }

                deltas[inArc.Source] -= inArc.Weight;
            }
        }

        return deltas;
    }
}