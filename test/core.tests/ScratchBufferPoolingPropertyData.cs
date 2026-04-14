namespace core.tests;

public sealed record ScratchBufferPlanningCase(GraphPetriNet Graph, MatrixPetriNet Matrix, Marking Marking);

public sealed record ScratchBufferFireCase(GraphPetriNet Graph, MatrixPetriNet Matrix, Marking Marking);

public static class ScratchBufferPoolingPropertyData
{
    public static ScratchBufferPlanningCase CreatePlanningCase(bool conflict, int leftPrioritySeed, int rightPrioritySeed)
    {
        var (graph, matrix, marking) = TransitionSelectionPropertyData.CreateEquivalentNetPair(
            conflict,
            leftPrioritySeed,
            rightPrioritySeed);

        return new ScratchBufferPlanningCase(graph, matrix, marking);
    }

    public static ScratchBufferFireCase CreateFireCase(int transitionCount)
    {
        var scenario = HotPathAllocationPropertyData.CreateFireCase(transitionCount);
        return new ScratchBufferFireCase(scenario.Graph, scenario.Matrix, scenario.Marking);
    }

    public static ScratchBufferFireCase CreateEmptyCase()
    {
        var placeNames = new Dictionary<int, string> { [0] = "p0" };
        var transitionNames = new Dictionary<int, string>();
        var inArcs = new Dictionary<int, List<InArc>>();
        var outArcs = new Dictionary<int, List<OutArc>>();

        var graph = new GraphPetriNet("graph-empty", placeNames, transitionNames, inArcs, outArcs);
        var matrix = new MatrixPetriNet("matrix-empty", placeNames, transitionNames, inArcs, outArcs);
        var marking = new Marking(1);

        return new ScratchBufferFireCase(graph, matrix, marking);
    }

    public static int[] Snapshot(Marking marking)
    {
        var snapshot = new int[marking.Size];
        for (var i = 0; i < marking.Size; i++)
        {
            snapshot[i] = marking[i];
        }

        return snapshot;
    }
}
