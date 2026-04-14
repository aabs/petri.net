namespace core.tests;

public class MatrixPetriNetStateEquationProperties
{
    [Property]
    public bool StateEquationDelta_IsExactForFiringPlanTransitionSet(PositiveInt placeSeed, PositiveInt transitionSeed, int seed)
    {
        var model = SparseMatrixKernelPropertyData.CreateModel(
            (placeSeed.Get % 14) + 2,
            (transitionSeed.Get % 14) + 1,
            seed);
        var nets = SparseMatrixKernelPropertyData.BuildNets(model);
        var transitionIds = nets.Matrix.CreateFiringPlan(model.Marking).TransitionIds;

        return SparseMatrixKernelParityOracle.StateEquationDeltaIsExact(model, transitionIds);
    }

    [Property]
    public bool StateEquationDelta_BaselineTransitionSemanticsMatchGraph(PositiveInt placeSeed, PositiveInt transitionSeed, int seed)
    {
        var model = SparseMatrixKernelPropertyData.CreateModel(
            (placeSeed.Get % 14) + 2,
            (transitionSeed.Get % 14) + 1,
            seed);
        var nets = SparseMatrixKernelPropertyData.BuildNets(model);
        var baselineTransitionSet = nets.Graph.AllEnabledTransitions(model.Marking).OrderBy(id => id).ToArray();

        return SparseMatrixKernelParityOracle.StateEquationDeltaIsExact(model, baselineTransitionSet);
    }
}
