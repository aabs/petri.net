using FsCheck;
using FsCheck.Xunit;
using petrinets2.core;
using Xunit;

namespace core.tests;

public class GraphPetriNetProperties
{
    [Property]
    public bool IsEnabled_MatchesModel_ForGeneratedSingleTransitionScenarios(NonNull<int[]> tokensRaw, NonNull<int[]> requiredRaw, NonNull<bool[]> inhibitorRaw, NonNull<int[]> outputRaw)
    {
        var scenario = BuildScenario(tokensRaw.Item, requiredRaw.Item, inhibitorRaw.Item, outputRaw.Item);
        var net = scenario.BuildNet();
        var marking = scenario.BuildMarking();

        var actual = net.IsEnabled(0, marking);
        var expected = scenario.ModelIsEnabled();

        return actual == expected;
    }

    [Property]
    public bool Fire_MatchesModelTransition_ForGeneratedSingleTransitionScenarios(NonNull<int[]> tokensRaw, NonNull<int[]> requiredRaw, NonNull<bool[]> inhibitorRaw, NonNull<int[]> outputRaw)
    {
        var scenario = BuildScenario(tokensRaw.Item, requiredRaw.Item, inhibitorRaw.Item, outputRaw.Item);
        var net = scenario.BuildNet();
        var marking = scenario.BuildMarking();
        var snapshot = scenario.Tokens.ToArray();

        var actual = net.Fire(marking);
        var expected = scenario.ModelFire();

        for (var i = 0; i < expected.Length; i++)
        {
            if (actual[i] != expected[i])
            {
                return false;
            }

            if (marking[i] != snapshot[i])
            {
                return false;
            }
        }

        return true;
    }

    [Property]
    public bool Fire_DisabledTransition_DoesNotInvokeCallbacks(NonNull<int[]> tokensRaw, NonNull<int[]> requiredRaw, NonNull<bool[]> inhibitorRaw, NonNull<int[]> outputRaw)
    {
        var scenario = BuildScenario(tokensRaw.Item, requiredRaw.Item, inhibitorRaw.Item, outputRaw.Item);
        if (scenario.ModelIsEnabled())
        {
            return true;
        }

        var net = scenario.BuildNet();
        var marking = scenario.BuildMarking();
        var calls = 0;
        net.RegisterFunction(0, _ => calls++);

        _ = net.Fire(marking);

        return calls == 0;
    }

    [Property]
    public bool GetWeight_ReturnsConfiguredInputWeights_ForGeneratedScenarios(NonNull<int[]> tokensRaw, NonNull<int[]> requiredRaw, NonNull<bool[]> inhibitorRaw, NonNull<int[]> outputRaw)
    {
        var scenario = BuildScenario(tokensRaw.Item, requiredRaw.Item, inhibitorRaw.Item, outputRaw.Item);
        var net = scenario.BuildNet();

        foreach (var required in scenario.RequiredWeights)
        {
            if (net.GetWeight(required.Key, 0) != required.Value)
            {
                return false;
            }
        }

        return true;
    }

    [Property]
    public bool EnabledSet_IsMonotonic_WhenNoInhibitors(NonNegativeInt seed)
    {
        var value = seed.Get;
        var placeCount = 3;

        var placeNames = Enumerable.Range(0, placeCount).ToDictionary(i => i, i => $"p{i}");
        var transitionNames = new Dictionary<int, string> { [0] = "t0", [1] = "t1", [2] = "t2" };

        var inArcs = new Dictionary<int, List<InArc>>
        {
            [0] = new List<InArc> { new(0, (value % 3) + 1, false), new(1, ((value / 3) % 3) + 1, false) },
            [1] = new List<InArc> { new(1, ((value / 9) % 3) + 1, false), new(2, ((value / 27) % 3) + 1, false) },
            [2] = new List<InArc> { new(0, ((value / 81) % 3) + 1, false), new(2, ((value / 243) % 3) + 1, false) }
        };

        var outArcs = new Dictionary<int, List<OutArc>>
        {
            [0] = new List<OutArc> { new(2, 1) },
            [1] = new List<OutArc> { new(0, 1) },
            [2] = new List<OutArc> { new(1, 1) }
        };

        var net = new GraphPetriNet("net", placeNames, transitionNames, inArcs, outArcs);

        var mLow = new Marking(placeCount);
        var mHigh = new Marking(placeCount);

        for (var i = 0; i < placeCount; i++)
        {
            var low = (value / (int)Math.Pow(7, i + 1)) % 4;
            var delta = (value / (int)Math.Pow(11, i + 1)) % 4;
            mLow[i] = low;
            mHigh[i] = low + delta;
        }

        var lowEnabled = net.AllEnabledTransitions(mLow).ToHashSet();
        var highEnabled = net.AllEnabledTransitions(mHigh).ToHashSet();

        return lowEnabled.IsSubsetOf(highEnabled);
    }

    [Property]
    public bool Fire_IsDeterministic_ForSameMarking(NonNegativeInt tokenSeed, PositiveInt weightSeed)
    {
        var tokens = (tokenSeed.Get % 16) + 4;
        var inWeight = (weightSeed.Get % 3) + 1;

        var placeNames = new Dictionary<int, string> { [0] = "p0", [1] = "p1" };
        var transitionNames = new Dictionary<int, string> { [0] = "t0" };
        var inArcs = new Dictionary<int, List<InArc>> { [0] = new List<InArc> { new(0, inWeight, false) } };
        var outArcs = new Dictionary<int, List<OutArc>> { [0] = new List<OutArc> { new(1, 2) } };

        var net = new GraphPetriNet("net", placeNames, transitionNames, inArcs, outArcs);

        var m1 = new Marking(2);
        m1[0] = tokens;
        var m2 = new Marking(m1);

        var r1 = net.Fire(m1);
        var r2 = net.Fire(m2);

        return r1[0] == r2[0] && r1[1] == r2[1];
    }

    [Property]
    public bool Constructor_NullArguments_ThrowPredictableExceptions(NonNegativeInt selectorSeed)
    {
        var selector = selectorSeed.Get % 5;

        var id = "net";
        var placeNames = new Dictionary<int, string> { [0] = "p0" };
        var transitionNames = new Dictionary<int, string> { [0] = "t0" };
        var inArcs = new Dictionary<int, List<InArc>> { [0] = new List<InArc> { new(0, inhibitor: false) } };
        var outArcs = new Dictionary<int, List<OutArc>> { [0] = new List<OutArc> { new(0) } };

        return selector switch
        {
            0 => Throws<ArgumentException>(() => _ = new GraphPetriNet(null!, placeNames, transitionNames, inArcs, outArcs)),
            1 => Throws<ArgumentNullException>(() => _ = new GraphPetriNet(id, null!, transitionNames, inArcs, outArcs)),
            2 => Throws<ArgumentNullException>(() => _ = new GraphPetriNet(id, placeNames, null!, inArcs, outArcs)),
            3 => Throws<ArgumentNullException>(() => _ = new GraphPetriNet(id, placeNames, transitionNames, null!, outArcs)),
            _ => Throws<ArgumentNullException>(() => _ = new GraphPetriNet(id, placeNames, transitionNames, inArcs, null!))
        };
    }

    [Property]
    public bool Constructor_EmptyOrWhitespaceId_ThrowsArgumentException(bool whitespace)
    {
        var id = whitespace ? "   " : string.Empty;

        return Throws<ArgumentException>(() =>
            _ = new GraphPetriNet(
                id,
                new Dictionary<int, string> { [0] = "p0" },
                new Dictionary<int, string> { [0] = "t0" },
                new Dictionary<int, List<InArc>>(),
                new Dictionary<int, List<OutArc>>()));
    }

    [Property]
    public bool Constructor_UnknownArcEndpoints_ThrowArgumentException(bool unknownInput)
    {
        var placeNames = new Dictionary<int, string> { [0] = "p0" };
        var transitionNames = new Dictionary<int, string> { [0] = "t0" };

        if (unknownInput)
        {
            var badIn = new Dictionary<int, List<InArc>> { [0] = new List<InArc> { new(99, 1, false) } };
            return Throws<ArgumentException>(() => _ = new GraphPetriNet("net", placeNames, transitionNames, badIn, new Dictionary<int, List<OutArc>>()));
        }

        var badOut = new Dictionary<int, List<OutArc>> { [0] = new List<OutArc> { new(99, 1) } };
        return Throws<ArgumentException>(() => _ = new GraphPetriNet("net", placeNames, transitionNames, new Dictionary<int, List<InArc>>(), badOut));
    }

    [Property]
    public bool AddArcFromPlace_MakesTransitionVisibleFromPlace(NonNegativeInt placeSeed, NonNegativeInt transitionSeed)
    {
        var place = placeSeed.Get % 16;
        var transition = transitionSeed.Get % 16;

        var net = CreateEmptyGraphNet();
        net.AddArcFromPlace(place, transition);

        return net.GetPlaceOutArcs(place).Contains(transition);
    }

    [Property]
    public bool AddArcIntoTransition_RegistersAsNonInhibitor(NonNegativeInt placeSeed, NonNegativeInt transitionSeed)
    {
        var place = placeSeed.Get % 16;
        var transition = transitionSeed.Get % 16;

        var net = CreateEmptyGraphNet();
        net.AddArcFromPlace(place, transition);
        net.AddArcIntoTransition(place, transition);

        return net.NonInhibitorsIntoTransition(transition).Contains(place)
               && !net.InhibitorsIntoTransition(transition).Contains(place);
    }

    [Property]
    public bool IsEmptyTransition_IsTrueWhenTransitionHasNoInputArcs(NonNegativeInt transitionSeed)
    {
        var transition = transitionSeed.Get % 32;
        var net = CreateEmptyGraphNet();

        return net.IsEmptyTransition(transition);
    }

    [Property]
    public bool IsEnabled_UsesInputArcWeight(NonNegativeInt tokensSeed, PositiveInt weightSeed)
    {
        var tokens = tokensSeed.Get % 16;
        var weight = (weightSeed.Get % 8) + 1;

        var placeNames = new Dictionary<int, string> { [0] = "p0", [1] = "p1" };
        var transitionNames = new Dictionary<int, string> { [0] = "t0" };
        var inArcs = new Dictionary<int, List<InArc>> { [0] = new List<InArc> { new(0, weight, false) } };
        var outArcs = new Dictionary<int, List<OutArc>> { [0] = new List<OutArc> { new(1) } };

        var net = new GraphPetriNet("net", placeNames, transitionNames, inArcs, outArcs);

        var marking = new Marking(2);
        marking[0] = tokens;

        return net.IsEnabled(0, marking) == (tokens >= weight);
    }

    [Property]
    public bool GetNextTransitionToFire_ChoosesHighestPriorityEnabledTransition(NonNegativeInt p1Seed, NonNegativeInt p2Seed)
    {
        var p1 = p1Seed.Get % 16;
        var p2 = (p2Seed.Get % 16) + 1;

        var placeNames = new Dictionary<int, string> { [0] = "p0", [1] = "p1" };
        var transitionNames = new Dictionary<int, string> { [1] = "t1", [2] = "t2" };
        var inArcs = new Dictionary<int, List<InArc>>
        {
            [1] = new List<InArc> { new(0, inhibitor: false) },
            [2] = new List<InArc> { new(0, inhibitor: false) }
        };
        var outArcs = new Dictionary<int, List<OutArc>>
        {
            [1] = new List<OutArc> { new(1) },
            [2] = new List<OutArc> { new(1) }
        };
        var ordering = new Dictionary<int, int>
        {
            [1] = p1,
            [2] = p2
        };

        var net = new GraphPetriNet("net", placeNames, transitionNames, inArcs, outArcs, ordering);
        net.AddArcFromPlace(0, 1);
        net.AddArcFromPlace(0, 2);

        var marking = new Marking(2);
        marking[0] = 2;

        var next = net.GetNextTransitionToFire(marking);
        var expected = p2 > p1 ? 2 : 1;

        return next == expected;
    }

    [Property]
    public bool Fire_EnabledTransition_ConsumesAndProducesUsingArcWeights(PositiveInt sourceTokensSeed, NonNegativeInt targetTokensSeed, PositiveInt inWeightSeed, PositiveInt outWeightSeed)
    {
        var inWeight = (inWeightSeed.Get % 4) + 1;
        var outWeight = (outWeightSeed.Get % 4) + 1;
        var sourceTokens = (sourceTokensSeed.Get % 16) + inWeight;
        var targetTokens = targetTokensSeed.Get % 16;

        var placeNames = new Dictionary<int, string> { [0] = "source", [1] = "target" };
        var transitionNames = new Dictionary<int, string> { [0] = "t0" };
        var inArcs = new Dictionary<int, List<InArc>> { [0] = new List<InArc> { new(0, inWeight, false) } };
        var outArcs = new Dictionary<int, List<OutArc>> { [0] = new List<OutArc> { new(1, outWeight) } };

        var net = new GraphPetriNet("net", placeNames, transitionNames, inArcs, outArcs);

        var marking = new Marking(2);
        marking[0] = sourceTokens;
        marking[1] = targetTokens;

        var result = net.Fire(marking);

         return result[0] == sourceTokens - inWeight
             && result[1] == targetTokens + outWeight
               && marking[0] == sourceTokens
               && marking[1] == targetTokens;
    }

    [Property]
    public bool Fire_DisabledByInhibitorTransition_ReturnsEquivalentMarking(PositiveInt sourceTokensSeed, NonNegativeInt targetTokensSeed)
    {
        var sourceTokens = (sourceTokensSeed.Get % 16) + 1;
        var targetTokens = targetTokensSeed.Get % 16;

        var placeNames = new Dictionary<int, string> { [0] = "source", [1] = "target" };
        var transitionNames = new Dictionary<int, string> { [0] = "t0" };
        var inArcs = new Dictionary<int, List<InArc>> { [0] = new List<InArc> { new(0, inhibitor: true) } };
        var outArcs = new Dictionary<int, List<OutArc>> { [0] = new List<OutArc> { new(1) } };

        var net = new GraphPetriNet("net", placeNames, transitionNames, inArcs, outArcs);

        var marking = new Marking(2);
        marking[0] = sourceTokens;
        marking[1] = targetTokens;

        var result = net.Fire(marking);

        return result[0] == sourceTokens && result[1] == targetTokens;
    }

    [Property]
    public bool Fire_InvokesRegisteredTransitionFunctionOnce(PositiveInt tokenSeed)
    {
        var sourceTokens = (tokenSeed.Get % 16) + 1;

        var placeNames = new Dictionary<int, string> { [0] = "source", [1] = "target" };
        var transitionNames = new Dictionary<int, string> { [0] = "t0" };
        var inArcs = new Dictionary<int, List<InArc>> { [0] = new List<InArc> { new(0, inhibitor: false) } };
        var outArcs = new Dictionary<int, List<OutArc>> { [0] = new List<OutArc> { new(1) } };

        var net = new GraphPetriNet("net", placeNames, transitionNames, inArcs, outArcs);
        net.AddArcFromPlace(0, 0);

        var called = 0;
        net.RegisterFunction(0, _ => called++);

        var marking = new Marking(2);
        marking[0] = sourceTokens;

        _ = net.Fire(marking);

        return called == 1;
    }

    [Property]
    public bool GetWeight_ThrowsForMissingPlace(NonNegativeInt placeSeed, NonNegativeInt transitionSeed)
    {
        var place = (placeSeed.Get % 16) + 1;
        var transition = transitionSeed.Get % 16;

        var net = CreateEmptyGraphNet();

        return Throws<KeyNotFoundException>(() => _ = net.GetWeight(place, transition));
    }

    [Property]
    public bool GetWeight_ThrowsForMissingTransitionFromExistingPlace(NonNegativeInt transitionSeed)
    {
        var transition = (transitionSeed.Get % 16) + 1;

        var net = CreateEmptyGraphNet();
        net.AddArcFromPlace(0, 0);

        return Throws<KeyNotFoundException>(() => _ = net.GetWeight(0, transition));
    }

    [Property]
    public bool ArcConstructors_NonPositiveWeight_ThrowArgumentOutOfRangeException(bool zero)
    {
        var weight = zero ? 0 : -1;

        return Throws<ArgumentOutOfRangeException>(() => _ = new InArc(0, weight, false))
               && Throws<ArgumentOutOfRangeException>(() => _ = new OutArc(0, weight));
    }

    [Property]
    public bool GetConflictedPlaces_ContainsPlaceWhenMultipleAdjacentTransitionsAreEnabled(NonNegativeInt tokensSeed)
    {
        var tokens = (tokensSeed.Get % 16) + 2;

        var placeNames = new Dictionary<int, string> { [0] = "p0", [1] = "p1" };
        var transitionNames = new Dictionary<int, string> { [1] = "t1", [2] = "t2" };
        var inArcs = new Dictionary<int, List<InArc>>
        {
            [1] = new List<InArc> { new(0, inhibitor: false) },
            [2] = new List<InArc> { new(0, inhibitor: false) }
        };
        var outArcs = new Dictionary<int, List<OutArc>>
        {
            [1] = new List<OutArc> { new(1) },
            [2] = new List<OutArc> { new(1) }
        };

        var net = new GraphPetriNet("net", placeNames, transitionNames, inArcs, outArcs);
        net.AddArcFromPlace(0, 1);
        net.AddArcFromPlace(0, 2);

        var marking = new Marking(2);
        marking[0] = tokens;

        return net.GetConflictedPlaces(marking).Contains(0);
    }

    private static GraphPetriNet CreateEmptyGraphNet()
    {
        return new GraphPetriNet(
            "net",
            new Dictionary<int, string> { [0] = "p0" },
            new Dictionary<int, string> { [0] = "t0" },
            new Dictionary<int, List<InArc>>(),
            new Dictionary<int, List<OutArc>>()
        );
    }

    private static bool Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
            return false;
        }
        catch (T)
        {
            return true;
        }
    }

    static SingleTransitionScenario BuildScenario(int[] tokensRaw, int[] requiredRaw, bool[] inhibitorRaw, int[] outputRaw)
    {
        var placeCount = tokensRaw.Length == 0 ? 1 : Math.Min(6, tokensRaw.Length);
        var tokens = new int[placeCount];
        var required = new Dictionary<int, int>();
        var inhibitors = new HashSet<int>();
        var outputs = new Dictionary<int, int>();

        for (var i = 0; i < placeCount; i++)
        {
            var tokenRaw = tokensRaw.Length == 0 ? 0 : tokensRaw[i % tokensRaw.Length];
            var tokenValue = tokenRaw == int.MinValue ? 0 : Math.Abs(tokenRaw % 25);
            tokens[i] = tokenValue;

            var requiredIndicator = requiredRaw.Length == 0 ? 0 : requiredRaw[i % requiredRaw.Length];
            var shouldAddRequired = requiredIndicator % 3 != 0;

            if (shouldAddRequired)
            {
                var weightRaw = requiredRaw.Length == 0 ? 1 : requiredRaw[(i + 1) % requiredRaw.Length];
                var safeWeight = weightRaw == int.MinValue ? 1 : Math.Abs(weightRaw % 5) + 1;
                required[i] = safeWeight;
            }

            var isInhibitor = inhibitorRaw.Length > 0 && inhibitorRaw[i % inhibitorRaw.Length];
            if (!shouldAddRequired && isInhibitor)
            {
                inhibitors.Add(i);
            }

            if (outputRaw.Length > 0)
            {
                var outputWeightRaw = outputRaw[i % outputRaw.Length];
                var outputWeight = outputWeightRaw == int.MinValue ? 0 : Math.Abs(outputWeightRaw % 6);
                if (outputWeight > 0)
                {
                    outputs[i] = outputWeight;
                }
            }
        }

        return new SingleTransitionScenario(tokens, required, inhibitors, outputs);
    }

    public sealed class SingleTransitionScenario
    {
        public SingleTransitionScenario(int[] tokens, Dictionary<int, int> requiredWeights, HashSet<int> inhibitors, Dictionary<int, int> outputWeights)
        {
            Tokens = tokens;
            RequiredWeights = requiredWeights;
            Inhibitors = inhibitors;
            OutputWeights = outputWeights;
        }

        public int[] Tokens { get; }
        public Dictionary<int, int> RequiredWeights { get; }
        public HashSet<int> Inhibitors { get; }
        public Dictionary<int, int> OutputWeights { get; }

        public GraphPetriNet BuildNet()
        {
            var placeNames = Enumerable.Range(0, Tokens.Length).ToDictionary(i => i, i => $"p{i}");
            var transitionNames = new Dictionary<int, string> { [0] = "t0" };

            var inArcList = new List<InArc>();
            foreach (var required in RequiredWeights)
            {
                inArcList.Add(new InArc(required.Key, required.Value, false));
            }
            foreach (var inhibitor in Inhibitors)
            {
                inArcList.Add(new InArc(inhibitor, 1, true));
            }

            var inArcs = new Dictionary<int, List<InArc>> { [0] = inArcList };
            var outArcList = OutputWeights.Select(o => new OutArc(o.Key, o.Value)).ToList();
            var outArcs = new Dictionary<int, List<OutArc>> { [0] = outArcList };

            return new GraphPetriNet("net", placeNames, transitionNames, inArcs, outArcs);
        }

        public Marking BuildMarking()
        {
            var m = new Marking(Tokens.Length);
            for (var i = 0; i < Tokens.Length; i++)
            {
                m[i] = Tokens[i];
            }

            return m;
        }

        public bool ModelIsEnabled()
        {
            var hasInputs = RequiredWeights.Count > 0 || Inhibitors.Count > 0;
            if (!hasInputs)
            {
                return true;
            }

            var inhibitorsSatisfied = Inhibitors.All(placeId => Tokens[placeId] == 0);
            var requiredSatisfied = RequiredWeights.All(r => Tokens[r.Key] >= r.Value);

            return inhibitorsSatisfied && requiredSatisfied;
        }

        public int[] ModelFire()
        {
            var next = Tokens.ToArray();
            if (!ModelIsEnabled())
            {
                return next;
            }

            foreach (var required in RequiredWeights)
            {
                next[required.Key] -= required.Value;
            }

            foreach (var output in OutputWeights)
            {
                next[output.Key] += output.Value;
            }

            return next;
        }
    }
}
