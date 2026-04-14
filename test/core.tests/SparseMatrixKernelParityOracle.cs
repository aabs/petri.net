namespace core.tests;

public static class SparseMatrixKernelParityOracle
{
    public static bool FireProducesEquivalentMarking(SparseKernelModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var nets = SparseMatrixKernelPropertyData.BuildNets(model);
        var graphAfter = nets.Graph.Fire(new Marking(model.Marking));
        var matrixAfter = nets.Matrix.Fire(new Marking(model.Marking));

        return Snapshot(graphAfter).SequenceEqual(Snapshot(matrixAfter));
    }

    public static bool EnabledTransitionSetIsEquivalent(SparseKernelModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var nets = SparseMatrixKernelPropertyData.BuildNets(model);
        var graphEnabled = nets.Graph.AllEnabledTransitions(model.Marking).OrderBy(x => x).ToArray();
        var matrixEnabled = nets.Matrix.GetEnabledTransitions(model.Marking).OrderBy(x => x).ToArray();

        return graphEnabled.SequenceEqual(matrixEnabled);
    }

    public static bool FireIsDeterministic(SparseKernelModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var nets = SparseMatrixKernelPropertyData.BuildNets(model);
        var first = Snapshot(nets.Matrix.Fire(new Marking(model.Marking)));
        var second = Snapshot(nets.Matrix.Fire(new Marking(model.Marking)));

        return first.SequenceEqual(second);
    }

    public static bool StateEquationDeltaIsExact(SparseKernelModel model, IEnumerable<int> transitionIds)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(transitionIds);

        var nets = SparseMatrixKernelPropertyData.BuildNets(model);
        var selected = transitionIds.ToArray();
        var graphDelta = ComputeGraphStateEquationDelta(nets.Graph, model.Marking, selected);
        var matrixDelta = ComputeMatrixStateEquationDelta(nets.Matrix, model.Marking, selected);

        return graphDelta.SequenceEqual(matrixDelta);
    }

    public static (int[] GraphTrace, int[] MatrixTrace) CaptureSideEffectOrder(SparseKernelModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var nets = SparseMatrixKernelPropertyData.BuildNets(model);
        var graphTrace = new List<int>();
        var matrixTrace = new List<int>();

        foreach (var transitionId in nets.Graph.Transitions.Keys)
        {
            nets.Graph.RegisterFunction(transitionId, id => graphTrace.Add(id));
            nets.Matrix.RegisterFunction(transitionId, id => matrixTrace.Add(id));
        }

        nets.Graph.Fire(new Marking(model.Marking));
        nets.Matrix.Fire(new Marking(model.Marking));

        return (graphTrace.ToArray(), matrixTrace.ToArray());
    }

    static int[] ComputeGraphStateEquationDelta(GraphPetriNet graph, Marking marking, IEnumerable<int> transitionIds)
    {
        var deltas = new int[marking.Size];
        foreach (var transitionId in transitionIds)
        {
            if (graph.OutArcs.TryGetValue(transitionId, out var outArcs))
            {
                foreach (var outArc in outArcs)
                {
                    deltas[outArc.Target] += outArc.Weight;
                }
            }

            if (!graph.InArcs.TryGetValue(transitionId, out var inArcs))
            {
                continue;
            }

            foreach (var inArc in inArcs)
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

    static int[] ComputeMatrixStateEquationDelta(MatrixPetriNet matrix, Marking marking, IEnumerable<int> transitionIds)
    {
        var deltas = new int[marking.Size];
        foreach (var transitionId in transitionIds)
        {
            for (var placeId = 0; placeId < marking.Size; placeId++)
            {
                deltas[placeId] += (int)matrix.OutMatrix[placeId, transitionId];

                var inWeight = matrix.InMatrix[placeId, transitionId];
                if (!double.IsNaN(inWeight))
                {
                    deltas[placeId] -= (int)inWeight;
                }
            }
        }

        return deltas;
    }

    static int[] Snapshot(Marking marking)
    {
        var values = new int[marking.Size];
        for (var placeId = 0; placeId < marking.Size; placeId++)
        {
            values[placeId] = marking[placeId];
        }

        return values;
    }
}