namespace core.benchmarks;

using petrinets2.core;

public enum DensityBand
{
    Low,
    Medium,
    High
}

public sealed record SparseMatrixFireScenario(GraphPetriNet Graph, MatrixPetriNet Matrix, Marking Marking);

public static class SparseMatrixBenchmarkScenarios
{
    public static SparseMatrixFireScenario CreateFireScenario(int transitionCount, DensityBand densityBand)
    {
        if (transitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionCount));
        }

        var placeCount = transitionCount * 2;
        var placeNames = Enumerable.Range(0, placeCount).ToDictionary(index => index, index => $"p{index}");
        var transitionNames = Enumerable.Range(0, transitionCount).ToDictionary(index => index, index => $"t{index}");
        var transitionOrdering = transitionNames.Keys.ToDictionary(index => index, index => index % 11);
        var inArcs = new Dictionary<int, List<InArc>>(transitionCount);
        var outArcs = new Dictionary<int, List<OutArc>>(transitionCount);
        var random = new Random(transitionCount * 31 + (int)densityBand);
        var connectionsPerTransition = GetConnectionsPerTransition(placeCount, densityBand);

        for (var transitionId = 0; transitionId < transitionCount; transitionId++)
        {
            var inputSources = TakeDistinctPlaces(placeCount, connectionsPerTransition, random);
            var outputTargets = TakeDistinctPlaces(placeCount, connectionsPerTransition, random);

            var transitionInArcs = new List<InArc>(connectionsPerTransition);
            foreach (var source in inputSources)
            {
                transitionInArcs.Add(new InArc(source, random.Next(1, 3), false));
            }

            var transitionOutArcs = new List<OutArc>(connectionsPerTransition);
            foreach (var target in outputTargets)
            {
                transitionOutArcs.Add(new OutArc(target, random.Next(1, 3)));
            }

            inArcs[transitionId] = transitionInArcs;
            outArcs[transitionId] = transitionOutArcs;
        }

        var graph = new GraphPetriNet("graph-sparse-fire", placeNames, transitionNames, inArcs, outArcs, transitionOrdering);
        var matrix = new MatrixPetriNet("matrix-sparse-fire", placeNames, transitionNames, inArcs, outArcs, transitionOrdering);
        var marking = new Marking(placeCount);
        for (var placeId = 0; placeId < placeCount; placeId++)
        {
            marking[placeId] = random.Next(2, 7);
        }

        return new SparseMatrixFireScenario(graph, matrix, marking);
    }

    static int GetConnectionsPerTransition(int placeCount, DensityBand densityBand)
    {
        return densityBand switch
        {
            DensityBand.Low => 1,
            DensityBand.Medium => Math.Max(2, placeCount / 10),
            DensityBand.High => Math.Max(2, placeCount / 4),
            _ => 1
        };
    }

    static int[] TakeDistinctPlaces(int placeCount, int count, Random random)
    {
        var values = new HashSet<int>();
        while (values.Count < count)
        {
            values.Add(random.Next(placeCount));
        }

        return values.ToArray();
    }
}