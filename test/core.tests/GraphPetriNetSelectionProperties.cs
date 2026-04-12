namespace core.tests;

public class GraphPetriNetSelectionProperties
{
    [Property]
    public bool EqualPriorityTie_UsesFirstEnabledTransition(NonNegativeInt prioritySeed)
    {
        var (graph, _, marking) = TransitionSelectionPropertyData.CreateEquivalentNetPair(true, prioritySeed.Get % 16, prioritySeed.Get % 16);
        return graph.GetNextTransitionToFire(marking) == 0;
    }

    [Property]
    public bool NoEnabledTransitions_ProducesEmptyPlan()
    {
        var (graph, _, _) = TransitionSelectionPropertyData.CreateEquivalentNetPair(false, 1, 2);
        var marking = TransitionSelectionPropertyData.ToMarking(0, 0, 0, 0);
        return graph.CreateFiringPlan(marking).IsEmpty && graph.GetNextTransitionToFire(marking) is null;
    }
}