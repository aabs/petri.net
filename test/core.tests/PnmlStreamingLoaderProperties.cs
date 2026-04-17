namespace core.tests;

public class PnmlStreamingLoaderProperties
{
    [Property]
    public bool Loader_ReturnsNetsInSourceOrder(PositiveInt netSeed)
    {
        var netCount = (netSeed.Get % 8) + 1;
        var scenario = PnmlStreamingPropertyData.CreateStreamingCase(netCount, 4, 4);
        var loader = new PnmlStreamingModelLoader();

        var graphIds = loader.LoadGraph(scenario.Path).Select(net => net.Id).ToArray();

        return graphIds.SequenceEqual(scenario.NetIds);
    }

    [Property]
    public bool Loader_GraphAndMatrixModesRemainBehaviorallyAligned(PositiveInt placeSeed, PositiveInt transitionSeed)
    {
        var placeCount = (placeSeed.Get % 12) + 2;
        var transitionCount = (transitionSeed.Get % 12) + 1;
        var scenario = PnmlStreamingPropertyData.CreateStreamingCase(2, placeCount, transitionCount);
        var loader = new PnmlStreamingModelLoader();

        var graphs = loader.LoadGraph(scenario.Path);
        var matrices = loader.LoadMatrix(scenario.Path);

        if (graphs.Count != matrices.Count)
        {
            return false;
        }

        for (var i = 0; i < graphs.Count; i++)
        {
            var graph = graphs[i];
            var matrix = matrices[i];

            if (!graph.Places.OrderBy(pair => pair.Key).SequenceEqual(matrix.Places.OrderBy(pair => pair.Key)))
            {
                return false;
            }

            if (!graph.Transitions.OrderBy(pair => pair.Key).SequenceEqual(matrix.Transitions.OrderBy(pair => pair.Key)))
            {
                return false;
            }

            if (graph.CreateInitialMarking().Size != matrix.CreateInitialMarking().Size)
            {
                return false;
            }
        }

        return true;
    }
}
