namespace core.tests;

public class ScratchBufferDeterminismProperties
{
    [Property]
    public bool RepeatedConcurrentInputs_ProduceDeterministicOutputs(PositiveInt transitionSeed, PositiveInt workerSeed)
    {
        var transitionCount = (transitionSeed.Get % 48) + 1;
        var workerCount = (workerSeed.Get % 8) + 2;
        var scenario = ScratchBufferContentionPropertyData.Create(transitionCount, workerCount, 8);

        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true);
        var snapshots = ScratchBufferContentionPropertyData.RunConcurrentGraphFireSnapshots(scenario);
        if (snapshots.Count == 0)
        {
            return true;
        }

        var baseline = snapshots[0];
        return snapshots.All(snapshot => snapshot.SequenceEqual(baseline));
    }

    [Property]
    public bool ReentrantNestedInvocation_IsSafe(PositiveInt transitionSeed)
    {
        var transitionCount = (transitionSeed.Get % 16) + 1;
        var scenario = ScratchBufferPoolingPropertyData.CreateFireCase(transitionCount);
        var entered = false;

        scenario.Graph.RegisterFunction(0, _ =>
        {
            if (entered)
            {
                return;
            }

            entered = true;
            scenario.Graph.Fire(scenario.Marking);
        });

        using var scope = ScratchBufferPooling.BeginTestScope(enablePooling: true);
        var first = ScratchBufferPoolingPropertyData.Snapshot(scenario.Graph.Fire(scenario.Marking));
        var second = ScratchBufferPoolingPropertyData.Snapshot(scenario.Graph.Fire(scenario.Marking));

        return first.SequenceEqual(second);
    }
}
