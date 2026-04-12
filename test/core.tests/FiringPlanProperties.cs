namespace core.tests;

public class FiringPlanProperties
{
    [Property]
    public bool EquivalentNets_DeriveEquivalentFiringPlans(bool conflict, NonNegativeInt leftPrioritySeed, NonNegativeInt rightPrioritySeed)
    {
        var (graph, matrix, marking) = TransitionSelectionPropertyData.CreateEquivalentNetPair(
            conflict,
            leftPrioritySeed.Get % 16,
            rightPrioritySeed.Get % 16);

        var graphPlan = graph.CreateFiringPlan(marking);
        var matrixPlan = matrix.CreateFiringPlan(marking);

        return graphPlan.TransitionIds.SequenceEqual(matrixPlan.TransitionIds);
    }

    [Property]
    public bool EmptyEnabledSet_ProducesEmptyFiringPlan(NonNegativeInt prioritySeed)
    {
        var (_, matrix, _) = TransitionSelectionPropertyData.CreateEquivalentNetPair(false, prioritySeed.Get % 8, 0);
        var emptyMarking = TransitionSelectionPropertyData.ToMarking(0, 0, 0, 0);

        return matrix.CreateFiringPlan(emptyMarking).IsEmpty;
    }
}