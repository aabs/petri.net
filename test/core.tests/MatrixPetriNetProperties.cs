namespace core.tests;

public class MatrixPetriNetProperties
{
    [Property]
    public bool Marking_IndexerRoundTripsAssignedValue(PositiveInt sizeSeed, int indexSeed, int value)
    {
        var size = Math.Min((sizeSeed.Get % 32) + 1, GraphPetriNet.MaxSize);
        var index = indexSeed == int.MinValue ? 0 : Math.Abs(indexSeed) % size;

        var marking = new Marking(size);
        marking[index] = value;

        return marking[index] == value;
    }

    [Property]
    public bool SingleInputTransition_EnablementMatchesTokenAvailability(NonNegativeInt tokenSeed)
    {
        var tokens = tokenSeed.Get % 16;
        var net = BuildNet(
            2,
            1,
            new Dictionary<int, List<InArc>> { { 0, new List<InArc> { new InArc(0) } } },
            new Dictionary<int, List<OutArc>> { { 0, new List<OutArc> { new OutArc(1) } } });

        var m = ToMarking(tokens, 0);
        return net.IsEnabled(0, m) == (tokens >= 1);
    }

    [Property]
    public bool TwoInputTransition_EnablementRequiresAllInputs(NonNegativeInt aSeed, NonNegativeInt bSeed)
    {
        var a = aSeed.Get % 4;
        var b = bSeed.Get % 4;

        var net = BuildNet(
            3,
            1,
            new Dictionary<int, List<InArc>> { { 0, new List<InArc> { new InArc(0), new InArc(2) } } },
            new Dictionary<int, List<OutArc>> { { 0, new List<OutArc> { new OutArc(1) } } });

        var m = ToMarking(a, 0, b);
        return net.IsEnabled(0, m) == (a >= 1 && b >= 1);
    }

    [Property]
    public bool SingleTransition_FireMatchesExpectedTokenDelta(NonNegativeInt srcSeed, NonNegativeInt dstSeed)
    {
        var src = (srcSeed.Get % 8) + 1;
        var dst = dstSeed.Get % 8;

        var net = BuildNet(
            2,
            1,
            new Dictionary<int, List<InArc>> { { 0, new List<InArc> { new InArc(0) } } },
            new Dictionary<int, List<OutArc>> { { 0, new List<OutArc> { new OutArc(1) } } });

        var before = ToMarking(src, dst);
        var after = net.Fire(before);

        return after[0] == before[0] - 1
               && after[1] == before[1] + 1;
    }

    [Property]
    public bool SelfLoopTransition_LeavesPlaceUnchanged(NonNegativeInt tokenSeed)
    {
        var tokens = tokenSeed.Get % 16;
        var net = BuildNet(
            1,
            1,
            new Dictionary<int, List<InArc>> { { 0, new List<InArc> { new InArc(0) } } },
            new Dictionary<int, List<OutArc>> { { 0, new List<OutArc> { new OutArc(0) } } });

        var before = ToMarking(tokens);
        var after = net.Fire(before);

        return after[0] == before[0];
    }

    [Property]
    public bool InputTransition_IncreasesByOnePerFire(NonNegativeInt initialSeed, PositiveInt stepsSeed)
    {
        var initial = initialSeed.Get % 16;
        var steps = (stepsSeed.Get % 8) + 1;

        var net = BuildNet(
            1,
            1,
            new Dictionary<int, List<InArc>>(),
            new Dictionary<int, List<OutArc>> { { 0, new List<OutArc> { new OutArc(0) } } });

        var m = ToMarking(initial);
        for (var i = 0; i < steps; i++)
        {
            m = net.Fire(m);
        }

        return m[0] == initial + steps;
    }

    [Property]
    public bool DrainTransition_DecreasesUntilZero(NonNegativeInt initialSeed, PositiveInt stepsSeed)
    {
        var initial = initialSeed.Get % 16;
        var steps = (stepsSeed.Get % 24) + 1;

        var net = BuildNet(
            1,
            1,
            new Dictionary<int, List<InArc>> { { 0, new List<InArc> { new InArc(0) } } },
            new Dictionary<int, List<OutArc>>());

        var m = ToMarking(initial);
        for (var i = 0; i < steps; i++)
        {
            var expectedEnabled = m[0] > 0;
            if (net.IsEnabled(0, m) != expectedEnabled)
            {
                return false;
            }

            m = net.Fire(m);
        }

        var expected = Math.Max(initial - steps, 0);
        return m[0] == expected;
    }

    [Property]
    public bool PriorityLookup_UsesConfiguredOrDefault(NonNegativeInt p0Seed, NonNegativeInt p1Seed)
    {
        var p0 = p0Seed.Get % 10;
        var p1 = p1Seed.Get % 10;

        var net = BuildNet(
            3,
            3,
            new Dictionary<int, List<InArc>>
            {
                { 0, new List<InArc> { new InArc(0) } },
                { 1, new List<InArc> { new InArc(1) } },
                { 2, new List<InArc> { new InArc(2) } }
            },
            new Dictionary<int, List<OutArc>>(),
            new Dictionary<int, int> { { 0, p0 }, { 1, p1 } });

        return net.GetTransitionPriority(0) == p0
               && net.GetTransitionPriority(1) == p1
               && net.GetTransitionPriority(2) == 0;
    }

    [Property]
    public bool ConflictDetection_MatchesSharedInputAndTokens(NonNegativeInt t0Seed, NonNegativeInt t1Seed, NonNegativeInt t2Seed)
    {
        var t0 = (t0Seed.Get % 3) + 1;
        var t1 = (t1Seed.Get % 3) + 1;
        var t2 = t2Seed.Get % 3;

        var net = BuildNet(
            3,
            2,
            new Dictionary<int, List<InArc>>
            {
                { 0, new List<InArc> { new InArc(0), new InArc(1) } },
                { 1, new List<InArc> { new InArc(1), new InArc(2) } }
            },
            new Dictionary<int, List<OutArc>>(),
            new Dictionary<int, int> { { 0, 1 }, { 1, 2 } });

        var m = ToMarking(t0, t1, t2);
        var enabled = net.GetEnabledTransitions(m).ToArray();

        var bothEnabled = enabled.Contains(0) && enabled.Contains(1);
        return net.IsConflicted(m) == bothEnabled;
    }

    [Property]
    public bool PrioritySelection_PicksHighestPriorityEnabledTransition(NonNegativeInt p0Seed, NonNegativeInt p1Seed)
    {
        var p0 = p0Seed.Get % 10;
        var p1 = p1Seed.Get % 10;

        var net = BuildNet(
            3,
            2,
            new Dictionary<int, List<InArc>>
            {
                { 0, new List<InArc> { new InArc(0), new InArc(1) } },
                { 1, new List<InArc> { new InArc(1), new InArc(2) } }
            },
            new Dictionary<int, List<OutArc>>(),
            new Dictionary<int, int> { { 0, p0 }, { 1, p1 } });

        var m = ToMarking(2, 2, 2);
        var selected = net.GetNextTransitionToFire(m);
        var expected = p0 >= p1 ? 0 : 1;

        return selected == expected;
    }

    [Property]
    public bool RegisteredTransitionFunction_ExecutesOncePerFire(PositiveInt initialSeed, PositiveInt stepsSeed)
    {
        var initial = (initialSeed.Get % 8) + 1;
        var steps = (stepsSeed.Get % 8) + 1;

        var net = BuildNet(
            1,
            1,
            new Dictionary<int, List<InArc>> { { 0, new List<InArc> { new InArc(0) } } },
            new Dictionary<int, List<OutArc>> { { 0, new List<OutArc> { new OutArc(0) } } });

        var callbackCount = 0;
        net.RegisterFunction(0, _ => callbackCount++);

        var m = ToMarking(initial);
        for (var i = 0; i < steps; i++)
        {
            m = net.Fire(m);
        }

        return callbackCount == steps;
    }

    [Fact]
    public void ComplexFlow_RegressionScenario()
    {
        var net = BuildNet(
            4,
            3,
            new Dictionary<int, List<InArc>>
            {
                { 0, new List<InArc> { new InArc(0) } },
                { 1, new List<InArc> { new InArc(1) } },
                { 2, new List<InArc> { new InArc(3) } }
            },
            new Dictionary<int, List<OutArc>>
            {
                { 0, new List<OutArc> { new OutArc(1) } },
                { 1, new List<OutArc> { new OutArc(2) } },
                { 2, new List<OutArc> { new OutArc(1) } }
            });

        var m = ToMarking(0, 0, 0, 0);
        m[0] = 1;

        m = net.Fire(m);
        Assert.Equal(new[] { 0, 1, 0, 0 }, Snapshot(m));

        m = net.Fire(m);
        Assert.Equal(new[] { 0, 0, 1, 0 }, Snapshot(m));

        m[3] = 1;
        m = net.Fire(m);
        Assert.Equal(new[] { 0, 1, 1, 0 }, Snapshot(m));
    }

    [Fact(Skip = "PNML loader is not present in the current codebase")]
    public void TestLoadPnmlFile()
    {
        Assert.True(true);
    }

    static MatrixPetriNet BuildNet(
        int placeCount,
        int transitionCount,
        Dictionary<int, List<InArc>> inArcs,
        Dictionary<int, List<OutArc>> outArcs,
        Dictionary<int, int>? transitionOrdering = null)
    {
        var places = Enumerable.Range(0, placeCount).ToDictionary(i => i, i => $"p{i}");
        var transitions = Enumerable.Range(0, transitionCount).ToDictionary(i => i, i => $"t{i}");

        if (transitionOrdering is null)
        {
            return new MatrixPetriNet("p", places, transitions, inArcs, outArcs);
        }

        return new MatrixPetriNet("p", places, transitions, inArcs, outArcs, transitionOrdering);
    }

    static Marking ToMarking(params int[] tokens)
    {
        var m = new Marking(tokens.Length);
        for (var i = 0; i < tokens.Length; i++)
        {
            m[i] = tokens[i];
        }

        return m;
    }

    static int[] Snapshot(Marking m)
    {
        var result = new int[m.Size];
        for (var i = 0; i < m.Size; i++)
        {
            result[i] = m[i];
        }

        return result;
    }
}
