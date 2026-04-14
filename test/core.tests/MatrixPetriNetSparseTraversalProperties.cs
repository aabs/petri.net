namespace core.tests;

public class MatrixPetriNetSparseTraversalProperties
{
    [Property]
    public bool NonZeroTraversal_FireParityMatchesGraph(PositiveInt placeSeed, PositiveInt transitionSeed, int seed)
    {
        var placeCount = (placeSeed.Get % 15) + 2;
        var transitionCount = (transitionSeed.Get % 16) + 1;
        var model = SparseMatrixKernelPropertyData.CreateModel(placeCount, transitionCount, seed);

        return SparseMatrixKernelParityOracle.FireProducesEquivalentMarking(model);
    }

    [Property]
    public bool DisconnectedPlace_RemainsUnchangedAfterFire(NonNegativeInt tokenSeed)
    {
        var tokens = tokenSeed.Get % 16;
        var places = new Dictionary<int, string> { [0] = "p0", [1] = "p1", [2] = "isolated" };
        var transitions = new Dictionary<int, string> { [0] = "t0" };
        var inArcs = new Dictionary<int, List<InArc>> { [0] = new List<InArc> { new(0, 1, false) } };
        var outArcs = new Dictionary<int, List<OutArc>> { [0] = new List<OutArc> { new(1, 1) } };
        var net = new MatrixPetriNet("matrix-disconnected", places, transitions, inArcs, outArcs);
        var marking = new Marking(places.Count);
        marking[0] = tokens + 1;
        marking[1] = 0;
        marking[2] = tokens;

        var after = net.Fire(marking);
        return after[2] == marking[2];
    }

    [Property]
    public bool MissingPaths_DoNotChangeUnrelatedPlaces(PositiveInt sourceSeed, NonNegativeInt isolatedSeed)
    {
        var sourceTokens = (sourceSeed.Get % 12) + 1;
        var isolatedTokens = isolatedSeed.Get % 12;
        var places = new Dictionary<int, string> { [0] = "source", [1] = "target", [2] = "unrelated" };
        var transitions = new Dictionary<int, string> { [0] = "t0" };
        var inArcs = new Dictionary<int, List<InArc>> { [0] = new List<InArc> { new(0, 1, false) } };
        var outArcs = new Dictionary<int, List<OutArc>> { [0] = new List<OutArc> { new(1, 1) } };
        var net = new MatrixPetriNet("matrix-missing-paths", places, transitions, inArcs, outArcs);
        var marking = new Marking(places.Count);
        marking[0] = sourceTokens;
        marking[2] = isolatedTokens;

        var after = net.Fire(marking);
        return after[2] == marking[2] && after[1] == marking[1] + 1;
    }

    [Property]
    public bool Fire_OnlyMutatesAffectedPlaces(PositiveInt placeSeed, PositiveInt transitionSeed, int seed)
    {
        var placeCount = (placeSeed.Get % 12) + 3;
        var transitionCount = (transitionSeed.Get % 10) + 1;
        var model = SparseMatrixKernelPropertyData.CreateModel(placeCount, transitionCount, seed);
        var nets = SparseMatrixKernelPropertyData.BuildNets(model);
        var before = new Marking(model.Marking);
        var firingPlan = nets.Matrix.CreateFiringPlan(before);
        var affectedPlaces = BuildAffectedPlaceSet(model, firingPlan.TransitionIds);

        var after = nets.Matrix.Fire(before);
        for (var placeId = 0; placeId < before.Size; placeId++)
        {
            if (affectedPlaces.Contains(placeId))
            {
                continue;
            }

            if (after[placeId] != before[placeId])
            {
                return false;
            }
        }

        return true;
    }

    static HashSet<int> BuildAffectedPlaceSet(SparseKernelModel model, IEnumerable<int> transitionIds)
    {
        var affected = new HashSet<int>();
        foreach (var transitionId in transitionIds)
        {
            if (model.InArcs.TryGetValue(transitionId, out var inArcs))
            {
                foreach (var inArc in inArcs)
                {
                    if (!inArc.IsInhibitor)
                    {
                        affected.Add(inArc.Source);
                    }
                }
            }

            if (!model.OutArcs.TryGetValue(transitionId, out var outArcs))
            {
                continue;
            }

            foreach (var outArc in outArcs)
            {
                affected.Add(outArc.Target);
            }
        }

        return affected;
    }
}
