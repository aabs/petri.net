using FsCheck;
using FsCheck.Xunit;
using petrinets2.core;
using Xunit;

namespace core.tests;

public class GraphPetriNetProperties
{
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

    [Fact]
    public void Constructor_UnknownArcEndpoints_ThrowArgumentException()
    {
        var placeNames = new Dictionary<int, string> { [0] = "p0" };
        var transitionNames = new Dictionary<int, string> { [0] = "t0" };

        var badIn = new Dictionary<int, List<InArc>> { [0] = new List<InArc> { new(99, 1, false) } };
        Assert.Throws<ArgumentException>(() => _ = new GraphPetriNet("net", placeNames, transitionNames, badIn, new Dictionary<int, List<OutArc>>()));

        var badOut = new Dictionary<int, List<OutArc>> { [0] = new List<OutArc> { new(99, 1) } };
        Assert.Throws<ArgumentException>(() => _ = new GraphPetriNet("net", placeNames, transitionNames, new Dictionary<int, List<InArc>>(), badOut));
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
    public bool GetWeight_ThrowsArgumentExceptionForMissingPlace(NonNegativeInt placeSeed, NonNegativeInt transitionSeed)
    {
        var place = (placeSeed.Get % 16) + 1;
        var transition = transitionSeed.Get % 16;

        var net = CreateEmptyGraphNet();

        return Throws<KeyNotFoundException>(() => _ = net.GetWeight(place, transition));
    }

    [Property]
    public bool GetWeight_ThrowsArgumentExceptionForMissingTransitionFromExistingPlace(NonNegativeInt transitionSeed)
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
}
