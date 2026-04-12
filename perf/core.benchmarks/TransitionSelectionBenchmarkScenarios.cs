namespace core.benchmarks;

using petrinets2.core;

public enum PriorityDistribution
{
    Flat,
    Skewed,
    Mixed
}

public sealed record TransitionSelectionScenario(GraphPetriNet Graph, MatrixPetriNet Matrix, Marking Marking);

public static class TransitionSelectionBenchmarkScenarios
{
    public static TransitionSelectionScenario Create(int transitionCount, PriorityDistribution distribution)
    {
        if (transitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionCount));
        }

        var placeNames = Enumerable.Range(0, transitionCount + 1).ToDictionary(index => index, index => $"p{index}");
        var transitionNames = Enumerable.Range(0, transitionCount).ToDictionary(index => index, index => $"t{index}");
        var transitionOrdering = transitionNames.Keys.ToDictionary(index => index, index => GetPriority(index, transitionCount, distribution));

        var inArcs = transitionNames.Keys.ToDictionary(
            index => index,
            index => new List<InArc> { new(0, 1, false) });

        var outArcs = transitionNames.Keys.ToDictionary(
            index => index,
            index => new List<OutArc> { new(index + 1, 1) });

        var graph = new GraphPetriNet("graph-benchmark", placeNames, transitionNames, inArcs, outArcs, transitionOrdering);
        var matrix = new MatrixPetriNet("matrix-benchmark", placeNames, transitionNames, inArcs, outArcs, transitionOrdering);
        var marking = new Marking(transitionCount + 1);
        marking[0] = transitionCount;

        return new TransitionSelectionScenario(graph, matrix, marking);
    }

    static int GetPriority(int transitionId, int transitionCount, PriorityDistribution distribution)
    {
        return distribution switch
        {
            PriorityDistribution.Flat => 0,
            PriorityDistribution.Skewed => transitionCount - transitionId,
            _ => (transitionId * 17) % 13
        };
    }
}