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

    [Property]
    public bool HotPathFireCase_MatrixMatchesGraphMarkingResult(PositiveInt transitionSeed)
    {
        var transitionCount = (transitionSeed.Get % 64) + 1;
        var scenario = HotPathAllocationPropertyData.CreateFireCase(transitionCount);

        var graphResult = scenario.Graph.Fire(scenario.Marking);
        var matrixResult = scenario.Matrix.Fire(scenario.Marking);

        return HotPathAllocationPropertyData.Snapshot(graphResult).SequenceEqual(HotPathAllocationPropertyData.Snapshot(matrixResult));
    }

    [Property]
    public bool HotPathFireCase_MatrixInvocationOrderMatchesGraph(PositiveInt transitionSeed)
    {
        var transitionCount = (transitionSeed.Get % 64) + 1;
        var scenario = HotPathAllocationPropertyData.CreateFireCase(transitionCount);

        var graphCalls = new List<int>();
        var matrixCalls = new List<int>();

        foreach (var transitionId in scenario.Graph.Transitions.Keys)
        {
            var captured = transitionId;
            scenario.Graph.RegisterFunction(captured, id => graphCalls.Add(id));
            scenario.Matrix.RegisterFunction(captured, id => matrixCalls.Add(id));
        }

        _ = scenario.Graph.Fire(scenario.Marking);
        _ = scenario.Matrix.Fire(scenario.Marking);

        return graphCalls.SequenceEqual(matrixCalls);
    }
}