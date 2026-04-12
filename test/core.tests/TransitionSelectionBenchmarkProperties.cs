namespace core.tests;

using core.benchmarks;

public class TransitionSelectionBenchmarkProperties
{
    [Property]
    public bool BenchmarkScenarioBuilder_IsDeterministic(PositiveInt countSeed)
    {
        var count = (countSeed.Get % 32) + 8;
        var first = TransitionSelectionBenchmarkScenarios.Create(count, PriorityDistribution.Mixed);
        var second = TransitionSelectionBenchmarkScenarios.Create(count, PriorityDistribution.Mixed);

        return first.Graph.GetNextTransitionToFire(first.Marking) == second.Graph.GetNextTransitionToFire(second.Marking)
            && first.Matrix.GetNextTransitionToFire(first.Marking) == second.Matrix.GetNextTransitionToFire(second.Marking);
    }
}