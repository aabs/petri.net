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
}