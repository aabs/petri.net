namespace core.tests;

using core.benchmarks;

public class HotPathAllocationBenchmarkEquivalenceProperties
{
    [Property]
    public bool ConflictScenarios_HaveGraphMatrixParity(PositiveInt branchSeed)
    {
        var branchCount = (branchSeed.Get % 32) + 2;
        var scenario = HotPathAllocationBenchmarkScenarios.CreateConflictScenario(branchCount);

        var graphConflict = scenario.Graph.PlaceIsConflicted(scenario.ConflictPlaceId, scenario.Marking);
        var matrixConflict = scenario.Matrix.PlaceIsConflicted(scenario.ConflictPlaceId, scenario.Marking);

        return graphConflict == matrixConflict;
    }

    [Property]
    public bool FireScenarios_HaveGraphMatrixParity(PositiveInt transitionSeed)
    {
        var transitionCount = (transitionSeed.Get % 64) + 1;
        var scenario = HotPathAllocationBenchmarkScenarios.CreateFireScenario(transitionCount);

        var graphResult = HotPathAllocationPropertyData.Snapshot(scenario.Graph.Fire(scenario.Marking));
        var matrixResult = HotPathAllocationPropertyData.Snapshot(scenario.Matrix.Fire(scenario.Marking));

        return graphResult.SequenceEqual(matrixResult);
    }
}
