namespace core.benchmarks;

using petrinets2.core;

public sealed record ScratchBufferPoolingPlanningScenario(GraphPetriNet Graph, MatrixPetriNet Matrix, Marking Marking);
public sealed record ScratchBufferPoolingFireScenario(GraphPetriNet Graph, MatrixPetriNet Matrix, Marking Marking);
public sealed record ScratchBufferPoolingContentionScenario(GraphPetriNet Graph, Marking Marking, int WorkerCount, int Iterations);

public static class ScratchBufferPoolingBenchmarkScenarios
{
    public static ScratchBufferPoolingPlanningScenario CreatePlanningScenario(int transitionCount)
    {
        var scenario = TransitionSelectionBenchmarkScenarios.Create(transitionCount, PriorityDistribution.Mixed);
        return new ScratchBufferPoolingPlanningScenario(scenario.Graph, scenario.Matrix, scenario.Marking);
    }

    public static ScratchBufferPoolingFireScenario CreateFireScenario(int transitionCount)
    {
        var scenario = HotPathAllocationBenchmarkScenarios.CreateFireScenario(transitionCount);
        return new ScratchBufferPoolingFireScenario(scenario.Graph, scenario.Matrix, scenario.Marking);
    }

    public static ScratchBufferPoolingContentionScenario CreateContentionScenario(int transitionCount, int workerCount, int iterations)
    {
        var fireScenario = CreateFireScenario(transitionCount);
        return new ScratchBufferPoolingContentionScenario(fireScenario.Graph, fireScenario.Marking, workerCount, iterations);
    }
}
