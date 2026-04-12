namespace core.tests;

using core.benchmarks;

public class HotPathAllocationBenchmarkProperties
{
    [Property]
    public bool ConflictScenarioBuilder_IsDeterministic(PositiveInt branchSeed)
    {
        var branchCount = (branchSeed.Get % 32) + 2;
        var first = HotPathAllocationBenchmarkScenarios.CreateConflictScenario(branchCount);
        var second = HotPathAllocationBenchmarkScenarios.CreateConflictScenario(branchCount);

        var firstGraphConflict = first.Graph.PlaceIsConflicted(first.ConflictPlaceId, first.Marking);
        var secondGraphConflict = second.Graph.PlaceIsConflicted(second.ConflictPlaceId, second.Marking);
        var firstMatrixConflict = first.Matrix.PlaceIsConflicted(first.ConflictPlaceId, first.Marking);
        var secondMatrixConflict = second.Matrix.PlaceIsConflicted(second.ConflictPlaceId, second.Marking);

        return firstGraphConflict == secondGraphConflict
            && firstMatrixConflict == secondMatrixConflict;
    }

    [Property]
    public bool FireScenarioBuilder_IsDeterministic(PositiveInt transitionSeed)
    {
        var transitionCount = (transitionSeed.Get % 64) + 1;
        var first = HotPathAllocationBenchmarkScenarios.CreateFireScenario(transitionCount);
        var second = HotPathAllocationBenchmarkScenarios.CreateFireScenario(transitionCount);

        var firstGraph = HotPathAllocationPropertyData.Snapshot(first.Graph.Fire(first.Marking));
        var secondGraph = HotPathAllocationPropertyData.Snapshot(second.Graph.Fire(second.Marking));
        var firstMatrix = HotPathAllocationPropertyData.Snapshot(first.Matrix.Fire(first.Marking));
        var secondMatrix = HotPathAllocationPropertyData.Snapshot(second.Matrix.Fire(second.Marking));

        return firstGraph.SequenceEqual(secondGraph)
            && firstMatrix.SequenceEqual(secondMatrix);
    }
}
