namespace core.tests;

public class MatrixPetriNetFiringPlanProperties
{
    [Property]
    public bool ConflictPlan_SelectsHighestPriorityTransition(NonNegativeInt leftPrioritySeed, NonNegativeInt rightPrioritySeed)
    {
        var (_, matrix, marking) = TransitionSelectionPropertyData.CreateEquivalentNetPair(true, leftPrioritySeed.Get % 16, rightPrioritySeed.Get % 16);
        var plan = matrix.CreateFiringPlan(marking);
        var expected = leftPrioritySeed.Get % 16 >= rightPrioritySeed.Get % 16 ? 0 : 1;

        return plan.TransitionIds.SequenceEqual(new[] { expected });
    }

    [Property]
    public bool NonConflictedPlan_IncludesAllEnabledTransitions(NonNegativeInt leftPrioritySeed, NonNegativeInt rightPrioritySeed)
    {
        var (_, matrix, marking) = TransitionSelectionPropertyData.CreateEquivalentNetPair(false, leftPrioritySeed.Get % 16, rightPrioritySeed.Get % 16);
        var plan = matrix.CreateFiringPlan(marking);

        return plan.TransitionIds.SequenceEqual(new[] { 0, 1 });
    }
}