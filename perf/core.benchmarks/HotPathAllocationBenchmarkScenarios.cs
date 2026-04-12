namespace core.benchmarks;

using petrinets2.core;

public sealed record HotPathAllocationScenario(GraphPetriNet Graph, MatrixPetriNet Matrix, Marking Marking, int ConflictPlaceId);

public static class HotPathAllocationBenchmarkScenarios
{
    public static HotPathAllocationScenario CreateConflictScenario(int branchCount)
    {
        if (branchCount < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(branchCount), "Branch count must be at least two.");
        }

        var placeNames = new Dictionary<int, string> { [0] = "shared" };
        for (var i = 0; i < branchCount; i++)
        {
            placeNames[i + 1] = $"target-{i}";
        }

        var transitionNames = new Dictionary<int, string>();
        var transitionOrdering = new Dictionary<int, int>();
        var inArcs = new Dictionary<int, List<InArc>>();
        var outArcs = new Dictionary<int, List<OutArc>>();

        for (var transitionId = 0; transitionId < branchCount; transitionId++)
        {
            transitionNames[transitionId] = $"t{transitionId}";
            transitionOrdering[transitionId] = branchCount - transitionId;
            inArcs[transitionId] = new List<InArc> { new(0, 1, false) };
            outArcs[transitionId] = new List<OutArc> { new(transitionId + 1, 1) };
        }

        var graph = new GraphPetriNet("graph-hotpath-conflict", placeNames, transitionNames, inArcs, outArcs, transitionOrdering);
        var matrix = new MatrixPetriNet("matrix-hotpath-conflict", placeNames, transitionNames, inArcs, outArcs, transitionOrdering);
        var marking = new Marking(placeNames.Count);
        marking[0] = branchCount;

        return new HotPathAllocationScenario(graph, matrix, marking, 0);
    }

    public static HotPathAllocationScenario CreateFireScenario(int transitionCount)
    {
        if (transitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionCount));
        }

        var placeNames = new Dictionary<int, string>();
        for (var placeId = 0; placeId < transitionCount * 2; placeId++)
        {
            placeNames[placeId] = $"p{placeId}";
        }

        var transitionNames = new Dictionary<int, string>();
        var transitionOrdering = new Dictionary<int, int>();
        var inArcs = new Dictionary<int, List<InArc>>();
        var outArcs = new Dictionary<int, List<OutArc>>();

        for (var transitionId = 0; transitionId < transitionCount; transitionId++)
        {
            var sourcePlace = transitionId;
            var targetPlace = transitionId + transitionCount;

            transitionNames[transitionId] = $"t{transitionId}";
            transitionOrdering[transitionId] = transitionId % 7;
            inArcs[transitionId] = new List<InArc> { new(sourcePlace, 1, false) };
            outArcs[transitionId] = new List<OutArc> { new(targetPlace, 1) };
        }

        var graph = new GraphPetriNet("graph-hotpath-fire", placeNames, transitionNames, inArcs, outArcs, transitionOrdering);
        var matrix = new MatrixPetriNet("matrix-hotpath-fire", placeNames, transitionNames, inArcs, outArcs, transitionOrdering);
        var marking = new Marking(placeNames.Count);
        for (var sourcePlace = 0; sourcePlace < transitionCount; sourcePlace++)
        {
            marking[sourcePlace] = 1;
        }

        return new HotPathAllocationScenario(graph, matrix, marking, 0);
    }
}
