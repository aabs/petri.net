namespace core.tests;

public sealed record SparseKernelModel(
    Dictionary<int, string> PlaceNames,
    Dictionary<int, string> TransitionNames,
    Dictionary<int, List<InArc>> InArcs,
    Dictionary<int, List<OutArc>> OutArcs,
    Dictionary<int, int> TransitionOrdering,
    Marking Marking,
    int[] SelectedTransitions);

public static class SparseMatrixKernelPropertyData
{
    public static SparseKernelModel CreateModel(int placeCount, int transitionCount, int seed)
    {
        if (placeCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placeCount));
        }

        if (transitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionCount));
        }

        var placeNames = Enumerable.Range(0, placeCount).ToDictionary(index => index, index => $"p{index}");
        var transitionNames = Enumerable.Range(0, transitionCount).ToDictionary(index => index, index => $"t{index}");
        var transitionOrdering = transitionNames.Keys.ToDictionary(index => index, index => (index * 7) % 5);
        var inArcs = new Dictionary<int, List<InArc>>(transitionCount);
        var outArcs = new Dictionary<int, List<OutArc>>(transitionCount);

        var random = new Random(seed);
        for (var transitionId = 0; transitionId < transitionCount; transitionId++)
        {
            var source = random.Next(placeCount);
            var target = random.Next(placeCount);
            var inputWeight = random.Next(1, 4);
            var outputWeight = random.Next(1, 4);

            inArcs[transitionId] =
            [
                new InArc(source, inputWeight, false)
            ];

            outArcs[transitionId] =
            [
                new OutArc(target, outputWeight)
            ];
        }

        var marking = new Marking(placeCount);
        for (var placeId = 0; placeId < placeCount; placeId++)
        {
            marking[placeId] = random.Next(0, 6);
        }

        var selectedTransitions = transitionNames.Keys.Where(transitionId => transitionId % 2 == 0).ToArray();

        return new SparseKernelModel(
            placeNames,
            transitionNames,
            inArcs,
            outArcs,
            transitionOrdering,
            marking,
            selectedTransitions);
    }

    public static (GraphPetriNet Graph, MatrixPetriNet Matrix) BuildNets(SparseKernelModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return (
            new GraphPetriNet(
                "graph-sparse-kernel",
                model.PlaceNames,
                model.TransitionNames,
                model.InArcs,
                model.OutArcs,
                model.TransitionOrdering),
            new MatrixPetriNet(
                "matrix-sparse-kernel",
                model.PlaceNames,
                model.TransitionNames,
                model.InArcs,
                model.OutArcs,
                model.TransitionOrdering));
    }
}