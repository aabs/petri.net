namespace core.tests;

public sealed record ScratchBufferContentionCase(GraphPetriNet Graph, MatrixPetriNet Matrix, Marking Marking, int WorkerCount, int Iterations);

public static class ScratchBufferContentionPropertyData
{
    public static ScratchBufferContentionCase Create(int transitionCount, int workerCount, int iterations)
    {
        var fireCase = ScratchBufferPoolingPropertyData.CreateFireCase(transitionCount);
        return new ScratchBufferContentionCase(fireCase.Graph, fireCase.Matrix, fireCase.Marking, workerCount, iterations);
    }

    public static IReadOnlyList<int[]> RunConcurrentGraphFireSnapshots(ScratchBufferContentionCase scenario)
    {
        var snapshots = new System.Collections.Concurrent.ConcurrentBag<int[]>();

        Parallel.For(
            0,
            scenario.WorkerCount,
            _ =>
            {
                for (var i = 0; i < scenario.Iterations; i++)
                {
                    var next = scenario.Graph.Fire(scenario.Marking);
                    snapshots.Add(ScratchBufferPoolingPropertyData.Snapshot(next));
                }
            });

        return snapshots.ToArray();
    }
}
