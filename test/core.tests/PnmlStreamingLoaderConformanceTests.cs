namespace core.tests;

public class PnmlStreamingLoaderConformanceTests
{
    [Property]
    public bool EmbeddedCorpus_LoadsInGraphAndMatrixModes()
    {
        var loader = new PnmlStreamingModelLoader();

        foreach (var path in PnmlStreamingPropertyData.EmbeddedCorpusPaths())
        {
            var graphNets = loader.LoadGraph(path);
            var matrixNets = loader.LoadMatrix(path);

            if (graphNets.Count == 0 || matrixNets.Count == 0)
            {
                return false;
            }

            if (graphNets.Count != matrixNets.Count)
            {
                return false;
            }
        }

        return true;
    }

    [Property]
    public bool DuplicatePlaceIds_ThrowDeterministically()
    {
        var loader = new PnmlStreamingModelLoader();
        var path = PnmlStreamingPropertyData.CreateDuplicatePlaceIdCase();

        var error = Record.Exception(() => loader.LoadGraph(path));

        return error is InvalidDataException invalidData
            && invalidData.Message.Contains("duplicate place", StringComparison.OrdinalIgnoreCase)
            && invalidData.Message.Contains("duplicate-place", StringComparison.Ordinal);
    }

    [Property]
    public bool MissingArcEndpoints_ThrowDeterministically()
    {
        var loader = new PnmlStreamingModelLoader();
        var path = PnmlStreamingPropertyData.CreateMissingArcEndpointCase();

        var error = Record.Exception(() => loader.LoadMatrix(path));

        return error is KeyNotFoundException keyNotFound
            && keyNotFound.Message.Contains("missing-endpoint", StringComparison.Ordinal)
            && keyNotFound.Message.Contains("T_missing", StringComparison.Ordinal);
    }
}
