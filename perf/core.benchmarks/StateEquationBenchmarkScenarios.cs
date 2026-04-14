namespace core.benchmarks;

using petrinets2.core;

public sealed record StateEquationScenario(
    GraphPetriNet Graph,
    MatrixPetriNet Matrix,
    Marking Marking,
    int[] TransitionIds,
    Dictionary<int, List<InArc>> InArcs,
    Dictionary<int, List<OutArc>> OutArcs);

public static class StateEquationBenchmarkScenarios
{
    public static StateEquationScenario Create(int transitionCount, DensityBand densityBand)
    {
        if (transitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionCount));
        }

        var fireScenario = SparseMatrixBenchmarkScenarios.CreateFireScenario(transitionCount, densityBand);
        var placeNames = fireScenario.Matrix.Places.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var transitionNames = fireScenario.Matrix.Transitions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var inArcs = BuildInArcMap(fireScenario.Matrix);
        var outArcs = BuildOutArcMap(fireScenario.Matrix);
        var transitionOrdering = transitionNames.Keys.ToDictionary(index => index, index => index % 13);

        var graph = new GraphPetriNet("graph-state-equation", placeNames, transitionNames, inArcs, outArcs, transitionOrdering);
        var matrix = new MatrixPetriNet("matrix-state-equation", placeNames, transitionNames, inArcs, outArcs, transitionOrdering);
        var transitionIds = transitionNames.Keys.Where(id => id % 2 == 0).ToArray();

        return new StateEquationScenario(graph, matrix, fireScenario.Marking, transitionIds, inArcs, outArcs);
    }

    public static int[] ComputeStateEquationDelta(StateEquationScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var deltas = new int[scenario.Marking.Size];
        foreach (var transitionId in scenario.TransitionIds)
        {
            if (scenario.OutArcs.TryGetValue(transitionId, out var transitionOutArcs))
            {
                foreach (var outArc in transitionOutArcs)
                {
                    deltas[outArc.Target] += outArc.Weight;
                }
            }

            if (!scenario.InArcs.TryGetValue(transitionId, out var transitionInArcs))
            {
                continue;
            }

            foreach (var inArc in transitionInArcs)
            {
                if (inArc.IsInhibitor)
                {
                    continue;
                }

                deltas[inArc.Source] -= inArc.Weight;
            }
        }

        return deltas;
    }

    static Dictionary<int, List<InArc>> BuildInArcMap(MatrixPetriNet matrix)
    {
        var result = new Dictionary<int, List<InArc>>();
        for (var transitionId = 0; transitionId < matrix.Transitions.Count; transitionId++)
        {
            var transitionInArcs = new List<InArc>();
            for (var placeId = 0; placeId < matrix.Places.Count; placeId++)
            {
                var value = matrix.InMatrix[placeId, transitionId];
                if (value == 0.0)
                {
                    continue;
                }

                var isInhibitor = double.IsNaN(value);
                var weight = isInhibitor ? 1 : (int)value;
                transitionInArcs.Add(new InArc(placeId, weight, isInhibitor));
            }

            result[transitionId] = transitionInArcs;
        }

        return result;
    }

    static Dictionary<int, List<OutArc>> BuildOutArcMap(MatrixPetriNet matrix)
    {
        var result = new Dictionary<int, List<OutArc>>();
        for (var transitionId = 0; transitionId < matrix.Transitions.Count; transitionId++)
        {
            var transitionOutArcs = new List<OutArc>();
            for (var placeId = 0; placeId < matrix.Places.Count; placeId++)
            {
                var value = matrix.OutMatrix[placeId, transitionId];
                if (value == 0.0)
                {
                    continue;
                }

                transitionOutArcs.Add(new OutArc(placeId, (int)value));
            }

            result[transitionId] = transitionOutArcs;
        }

        return result;
    }
}