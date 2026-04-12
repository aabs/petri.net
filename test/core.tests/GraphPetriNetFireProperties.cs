namespace core.tests;

public class GraphPetriNetFireProperties
{
    [Property]
    public bool EquivalentNets_FireToEquivalentMarkings_AndDispatchSameHandlers(bool conflict, NonNegativeInt leftPrioritySeed, NonNegativeInt rightPrioritySeed)
    {
        var (graph, matrix, marking) = TransitionSelectionPropertyData.CreateEquivalentNetPair(
            conflict,
            leftPrioritySeed.Get % 16,
            rightPrioritySeed.Get % 16);

        var graphCalls = new List<int>();
        var matrixCalls = new List<int>();

        graph.RegisterFunction(0, id => graphCalls.Add(id));
        graph.RegisterFunction(1, id => graphCalls.Add(id));
        matrix.RegisterFunction(0, id => matrixCalls.Add(id));
        matrix.RegisterFunction(1, id => matrixCalls.Add(id));

        var graphResult = graph.Fire(marking);
        var matrixResult = matrix.Fire(marking);

        return TransitionSelectionPropertyData.Snapshot(graphResult).SequenceEqual(TransitionSelectionPropertyData.Snapshot(matrixResult))
            && graphCalls.SequenceEqual(matrixCalls);
    }

    [Property]
    public bool HotPathFireCase_FireParityAndInvocationOrderArePreserved(PositiveInt transitionSeed)
    {
        var transitionCount = (transitionSeed.Get % 64) + 1;
        var scenario = HotPathAllocationPropertyData.CreateFireCase(transitionCount);

        var graphCalls = new List<int>();
        var matrixCalls = new List<int>();

        foreach (var transitionId in scenario.Graph.Transitions.Keys)
        {
            var captured = transitionId;
            scenario.Graph.RegisterFunction(captured, id => graphCalls.Add(id));
            scenario.Matrix.RegisterFunction(captured, id => matrixCalls.Add(id));
        }

        var graphResult = scenario.Graph.Fire(scenario.Marking);
        var matrixResult = scenario.Matrix.Fire(scenario.Marking);

        return HotPathAllocationPropertyData.Snapshot(graphResult).SequenceEqual(HotPathAllocationPropertyData.Snapshot(matrixResult))
            && graphCalls.SequenceEqual(matrixCalls);
    }
}