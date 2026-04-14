namespace core.tests;

public class MatrixPetriNetParityProperties
{
    [Property]
    public bool PostFireMarking_ParityMatchesGraph(PositiveInt placeSeed, PositiveInt transitionSeed, int seed)
    {
        var model = SparseMatrixKernelPropertyData.CreateModel(
            (placeSeed.Get % 15) + 2,
            (transitionSeed.Get % 16) + 1,
            seed);

        return SparseMatrixKernelParityOracle.FireProducesEquivalentMarking(model);
    }

    [Property]
    public bool SideEffectInvocationOrder_MatchesGraph(PositiveInt placeSeed, PositiveInt transitionSeed, int seed)
    {
        var model = SparseMatrixKernelPropertyData.CreateModel(
            (placeSeed.Get % 12) + 2,
            (transitionSeed.Get % 12) + 1,
            seed);

        var traces = SparseMatrixKernelParityOracle.CaptureSideEffectOrder(model);
        return traces.GraphTrace.SequenceEqual(traces.MatrixTrace);
    }

    [Property]
    public bool EnablementAndConflict_OutcomesMatchGraph(PositiveInt placeSeed, PositiveInt transitionSeed, int seed)
    {
        var model = SparseMatrixKernelPropertyData.CreateModel(
            (placeSeed.Get % 12) + 2,
            (transitionSeed.Get % 10) + 1,
            seed);
        var nets = SparseMatrixKernelPropertyData.BuildNets(model);

        var graphEnabled = nets.Graph.AllEnabledTransitions(model.Marking).OrderBy(id => id).ToArray();
        var matrixEnabled = nets.Matrix.GetEnabledTransitions(model.Marking).OrderBy(id => id).ToArray();

        return graphEnabled.SequenceEqual(matrixEnabled)
               && nets.Graph.IsConflicted(model.Marking) == nets.Matrix.IsConflicted(model.Marking);
    }

    [Property]
    public bool MatrixFire_IsDeterministicForIdenticalInputs(PositiveInt placeSeed, PositiveInt transitionSeed, int seed)
    {
        var model = SparseMatrixKernelPropertyData.CreateModel(
            (placeSeed.Get % 12) + 2,
            (transitionSeed.Get % 10) + 1,
            seed);

        return SparseMatrixKernelParityOracle.FireIsDeterministic(model);
    }
}
