namespace core.tests;

using core.benchmarks;

public class TransitionSelectionBenchmarkEquivalenceProperties
{
    [Property]
    public bool BenchmarkScenarios_SelectEquivalentTransitions(PositiveInt countSeed, NonNegativeInt distributionSeed)
    {
        var count = (countSeed.Get % 32) + 8;
        var distribution = (PriorityDistribution)(distributionSeed.Get % 3);
        var scenario = TransitionSelectionBenchmarkScenarios.Create(count, distribution);

        return scenario.Graph.GetNextTransitionToFire(scenario.Marking) == scenario.Matrix.GetNextTransitionToFire(scenario.Marking);
    }
}