namespace core.tests;

public class ScratchBufferPoolingReuseProperties
{
    [Property]
    public bool SteadyStateReuse_DoesNotRequireFreshBufferPerCycle(PositiveInt transitionSeed, PositiveInt cycleSeed)
    {
        var transitionCount = (transitionSeed.Get % 48) + 1;
        var cycles = (cycleSeed.Get % 24) + 8;
        var scenario = ScratchBufferPoolingPropertyData.CreateFireCase(transitionCount);

        return ScratchBufferPoolingOracle.SteadyStateReuseObserved(scenario, cycles);
    }

    [Property]
    public bool EmptyAndZeroWorkload_EdgeCasesRemainStable()
    {
        var scenario = ScratchBufferPoolingPropertyData.CreateEmptyCase();
        var before = ScratchBufferPoolingPropertyData.Snapshot(scenario.Marking);

        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true);
        var afterGraph = ScratchBufferPoolingPropertyData.Snapshot(scenario.Graph.Fire(scenario.Marking));
        var afterMatrix = ScratchBufferPoolingPropertyData.Snapshot(scenario.Matrix.Fire(scenario.Marking));

        return before.SequenceEqual(afterGraph) && afterGraph.SequenceEqual(afterMatrix);
    }
}
