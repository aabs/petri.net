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

    [Property]
    public bool HotPathFireCase_GraphEnablementMatchesMatrixAcrossRepeatedChecks(PositiveInt transitionSeed)
    {
        var transitionCount = (transitionSeed.Get % 64) + 1;
        var scenario = HotPathAllocationPropertyData.CreateFireCase(transitionCount);

        var graphEnabledFirst = scenario.Graph.AllEnabledTransitions(scenario.Marking).OrderBy(x => x).ToArray();
        var graphEnabledSecond = scenario.Graph.AllEnabledTransitions(scenario.Marking).OrderBy(x => x).ToArray();
        var matrixEnabled = scenario.Matrix.GetEnabledTransitions(scenario.Marking).OrderBy(x => x).ToArray();

        return graphEnabledFirst.SequenceEqual(graphEnabledSecond)
            && graphEnabledFirst.SequenceEqual(matrixEnabled);
    }
}