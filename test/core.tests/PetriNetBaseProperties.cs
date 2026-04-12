using FsCheck;
using FsCheck.Xunit;
using petrinets2.core;

namespace core.tests;

public class PetriNetBaseProperties
{
    [Property]
    public bool IsEnabled_MatchesIndependentModel(NonNull<int[]> markingRaw, NonNull<int[]> graphRaw, NonNegativeInt transitionSeed)
    {
        var scenario = BuildScenario(markingRaw.Item, graphRaw.Item, includeInhibitors: true);
        var transition = scenario.Transitions[transitionSeed.Get % scenario.Transitions.Count];

        var actual = scenario.Net.IsEnabled(transition, scenario.Marking);
        var expected = scenario.ModelIsEnabled(transition, scenario.MarkingVector);

        return actual == expected;
    }

    [Property]
    public bool EmptyTransition_IsAlwaysEnabled(NonNull<int[]> markingRaw)
    {
        var places = new[] { 0, 1, 2 };
        var transitions = new[] { 0 };
        var marking = new Marking(places.Length);

        for (var i = 0; i < places.Length; i++)
        {
            var v = markingRaw.Item.Length == 0 ? 0 : markingRaw.Item[i % markingRaw.Item.Length];
            marking[i] = v == int.MinValue ? 0 : Math.Abs(v % 16);
        }

        var net = new GeneratedPetriNet(
            places,
            transitions,
            new Dictionary<int, HashSet<int>>(),
            new Dictionary<int, HashSet<int>>(),
            new Dictionary<(int place, int transition), int>(),
            new Dictionary<int, HashSet<int>>(),
            new HashSet<int> { 0 });

        return net.IsEnabled(0, marking);
    }

    [Property]
    public bool EnabledSet_IsMonotonic_WhenNoInhibitors(NonNull<int[]> lowRaw, NonNull<int[]> deltaRaw, NonNull<int[]> graphRaw)
    {
        var scenario = BuildScenario(lowRaw.Item, graphRaw.Item, includeInhibitors: false);

        var high = new int[scenario.MarkingVector.Length];
        for (var i = 0; i < high.Length; i++)
        {
            var deltaSource = deltaRaw.Item.Length == 0 ? 0 : deltaRaw.Item[i % deltaRaw.Item.Length];
            var delta = deltaSource == int.MinValue ? 0 : Math.Abs(deltaSource % 8);
            high[i] = scenario.MarkingVector[i] + delta;
        }

        var lowMarking = VectorToMarking(scenario.MarkingVector);
        var highMarking = VectorToMarking(high);

        var lowEnabled = scenario.Transitions.Where(t => scenario.Net.IsEnabled(t, lowMarking)).ToHashSet();
        var highEnabled = scenario.Transitions.Where(t => scenario.Net.IsEnabled(t, highMarking)).ToHashSet();

        return lowEnabled.IsSubsetOf(highEnabled);
    }

    [Property]
    public bool InhibitorOnlyTransitions_AreAntiMonotonicInTokens(NonNull<int[]> lowRaw, NonNull<int[]> deltaRaw)
    {
        var placeCount = lowRaw.Item.Length == 0 ? 1 : Math.Min(lowRaw.Item.Length, 6);
        var places = Enumerable.Range(0, placeCount).ToArray();
        var transitions = new[] { 0 };

        var inhibitors = new Dictionary<int, HashSet<int>>
        {
            [0] = places.Where(p => p % 2 == 0).ToHashSet()
        };
        if (inhibitors[0].Count == 0)
        {
            inhibitors[0].Add(0);
        }

        var nonInhibitors = new Dictionary<int, HashSet<int>>();
        var weights = new Dictionary<(int place, int transition), int>();
        var placeOutArcs = places.ToDictionary(p => p, _ => new HashSet<int> { 0 });
        var emptyTransitions = new HashSet<int>();

        var net = new GeneratedPetriNet(places, transitions, inhibitors, nonInhibitors, weights, placeOutArcs, emptyTransitions);

        var low = new int[placeCount];
        var high = new int[placeCount];
        for (var i = 0; i < placeCount; i++)
        {
            var baseRaw = lowRaw.Item.Length == 0 ? 0 : lowRaw.Item[i % lowRaw.Item.Length];
            var baseValue = baseRaw == int.MinValue ? 0 : Math.Abs(baseRaw % 4);
            var deltaRawValue = deltaRaw.Item.Length == 0 ? 0 : deltaRaw.Item[i % deltaRaw.Item.Length];
            var delta = deltaRawValue == int.MinValue ? 0 : Math.Abs(deltaRawValue % 4);

            low[i] = baseValue;
            high[i] = baseValue + delta;
        }

        var lowEnabled = net.IsEnabled(0, VectorToMarking(low));
        var highEnabled = net.IsEnabled(0, VectorToMarking(high));

        return !highEnabled || lowEnabled;
    }

    [Property]
    public bool PlaceConflict_MatchesModelAdjacentEnabledCount(NonNull<int[]> markingRaw, NonNull<int[]> graphRaw, NonNegativeInt placeSeed)
    {
        var scenario = BuildScenario(markingRaw.Item, graphRaw.Item, includeInhibitors: true);
        var place = scenario.Places[placeSeed.Get % scenario.Places.Count];

        var actual = scenario.Net.PlaceIsConflicted(place, scenario.Marking);
        var expectedEnabledAdjacent = scenario.Transitions
            .Where(t => scenario.PlaceOutArcs.TryGetValue(place, out var tos) && tos.Contains(t))
            .Count(t => scenario.ModelIsEnabled(t, scenario.MarkingVector));

        return actual == (expectedEnabledAdjacent > 1);
    }

    [Property]
    public bool HotPathConflictCase_AdjacentEnabledMatchesConflictExpectation(PositiveInt branchSeed)
    {
        var branchCount = (branchSeed.Get % 32) + 2;
        var scenario = HotPathAllocationPropertyData.CreateConflictCase(branchCount);
        var placeId = scenario.ConflictPlaceId;

        var adjacentGraph = scenario.Graph.GetEnabledTransitionsAdjacentToPlace(placeId, scenario.Marking).ToArray();
        var adjacentMatrix = scenario.Matrix.GetEnabledTransitionsAdjacentToPlace(placeId, scenario.Marking).ToArray();

        return adjacentGraph.Length == branchCount
            && adjacentMatrix.Length == branchCount
            && scenario.Graph.PlaceIsConflicted(placeId, scenario.Marking)
            && scenario.Matrix.PlaceIsConflicted(placeId, scenario.Marking);
    }

    [Property]
    public bool CreateInitialMarking_UsesAllPlacesCount(NonNull<int[]> markingRaw, NonNull<int[]> graphRaw)
    {
        var scenario = BuildScenario(markingRaw.Item, graphRaw.Item, includeInhibitors: true);
        var initial = scenario.Net.CreateInitialMarking();

        if (initial.Size != scenario.Places.Count)
        {
            return false;
        }

        for (var i = 0; i < initial.Size; i++)
        {
            if (initial[i] != 0)
            {
                return false;
            }
        }

        return true;
    }

    static Scenario BuildScenario(int[] markingRaw, int[] graphRaw, bool includeInhibitors)
    {
        var placeCount = markingRaw.Length == 0 ? 1 : Math.Min(markingRaw.Length, 6);
        var transitionCount = graphRaw.Length == 0 ? 1 : Math.Min(graphRaw.Length, 5);

        var places = Enumerable.Range(0, placeCount).ToArray();
        var transitions = Enumerable.Range(0, transitionCount).ToArray();

        var inhibitors = new Dictionary<int, HashSet<int>>();
        var nonInhibitors = new Dictionary<int, HashSet<int>>();
        var weights = new Dictionary<(int place, int transition), int>();
        var placeOutArcs = new Dictionary<int, HashSet<int>>();
        var emptyTransitions = new HashSet<int>();

        for (var t = 0; t < transitionCount; t++)
        {
            var hasAnyArc = false;
            for (var p = 0; p < placeCount; p++)
            {
                var raw = graphRaw.Length == 0 ? 0 : graphRaw[(t * placeCount + p) % graphRaw.Length];
                var code = raw == int.MinValue ? 0 : Math.Abs(raw % 7);

                if (includeInhibitors && code == 1)
                {
                    if (!inhibitors.ContainsKey(t))
                    {
                        inhibitors[t] = new HashSet<int>();
                    }
                    inhibitors[t].Add(p);
                    hasAnyArc = true;
                    AddPlaceOutArc(placeOutArcs, p, t);
                }
                else if (code >= 2)
                {
                    if (!nonInhibitors.ContainsKey(t))
                    {
                        nonInhibitors[t] = new HashSet<int>();
                    }
                    nonInhibitors[t].Add(p);
                    weights[(p, t)] = ((code - 1) % 5) + 1;
                    hasAnyArc = true;
                    AddPlaceOutArc(placeOutArcs, p, t);
                }
            }

            if (!hasAnyArc)
            {
                emptyTransitions.Add(t);
            }
        }

        var markingVector = new int[placeCount];
        for (var p = 0; p < placeCount; p++)
        {
            var raw = markingRaw.Length == 0 ? 0 : markingRaw[p % markingRaw.Length];
            markingVector[p] = raw == int.MinValue ? 0 : Math.Abs(raw % 16);
        }

        var net = new GeneratedPetriNet(places, transitions, inhibitors, nonInhibitors, weights, placeOutArcs, emptyTransitions);

        return new Scenario(net, places.ToList(), transitions.ToList(), markingVector, VectorToMarking(markingVector), inhibitors, nonInhibitors, weights, placeOutArcs, emptyTransitions);
    }

    static void AddPlaceOutArc(Dictionary<int, HashSet<int>> placeOutArcs, int place, int transition)
    {
        if (!placeOutArcs.ContainsKey(place))
        {
            placeOutArcs[place] = new HashSet<int>();
        }
        placeOutArcs[place].Add(transition);
    }

    static Marking VectorToMarking(int[] vector)
    {
        var m = new Marking(vector.Length);
        for (var i = 0; i < vector.Length; i++)
        {
            m[i] = vector[i];
        }
        return m;
    }

    sealed class Scenario
    {
        public Scenario(
            GeneratedPetriNet net,
            List<int> places,
            List<int> transitions,
            int[] markingVector,
            Marking marking,
            Dictionary<int, HashSet<int>> inhibitors,
            Dictionary<int, HashSet<int>> nonInhibitors,
            Dictionary<(int place, int transition), int> weights,
            Dictionary<int, HashSet<int>> placeOutArcs,
            HashSet<int> emptyTransitions)
        {
            Net = net;
            Places = places;
            Transitions = transitions;
            MarkingVector = markingVector;
            Marking = marking;
            Inhibitors = inhibitors;
            NonInhibitors = nonInhibitors;
            Weights = weights;
            PlaceOutArcs = placeOutArcs;
            EmptyTransitions = emptyTransitions;
        }

        public GeneratedPetriNet Net { get; }
        public List<int> Places { get; }
        public List<int> Transitions { get; }
        public int[] MarkingVector { get; }
        public Marking Marking { get; }
        public Dictionary<int, HashSet<int>> Inhibitors { get; }
        public Dictionary<int, HashSet<int>> NonInhibitors { get; }
        public Dictionary<(int place, int transition), int> Weights { get; }
        public Dictionary<int, HashSet<int>> PlaceOutArcs { get; }
        public HashSet<int> EmptyTransitions { get; }

        public bool ModelIsEnabled(int transitionId, int[] marking)
        {
            if (EmptyTransitions.Contains(transitionId))
            {
                return true;
            }

            var inhibitorsOk = !Inhibitors.TryGetValue(transitionId, out var inhib)
                               || inhib.All(placeId => marking[placeId] == 0);

            var nonInhibitorsOk = !NonInhibitors.TryGetValue(transitionId, out var req)
                                  || req.All(placeId => marking[placeId] >= Weights[(placeId, transitionId)]);

            return inhibitorsOk && nonInhibitorsOk;
        }
    }

    sealed class GeneratedPetriNet : PetriNetBase
    {
        readonly int[] places;
        readonly int[] transitions;
        readonly Dictionary<int, HashSet<int>> inhibitors;
        readonly Dictionary<int, HashSet<int>> nonInhibitors;
        readonly Dictionary<(int place, int transition), int> weights;
        readonly Dictionary<int, HashSet<int>> placeOutArcs;
        readonly HashSet<int> emptyTransitions;

        public GeneratedPetriNet(
            int[] places,
            int[] transitions,
            Dictionary<int, HashSet<int>> inhibitors,
            Dictionary<int, HashSet<int>> nonInhibitors,
            Dictionary<(int place, int transition), int> weights,
            Dictionary<int, HashSet<int>> placeOutArcs,
            HashSet<int> emptyTransitions)
        {
            this.places = places;
            this.transitions = transitions;
            this.inhibitors = inhibitors;
            this.nonInhibitors = nonInhibitors;
            this.weights = weights;
            this.placeOutArcs = placeOutArcs;
            this.emptyTransitions = emptyTransitions;
        }

        public override IEnumerable<int> AllPlaces()
        {
            return places;
        }

        public override IEnumerable<int> InhibitorsIntoTransition(int transitionId)
        {
            return inhibitors.TryGetValue(transitionId, out var items) ? items : Enumerable.Empty<int>();
        }

        public override IEnumerable<int> NonInhibitorsIntoTransition(int transitionId)
        {
            return nonInhibitors.TryGetValue(transitionId, out var items) ? items : Enumerable.Empty<int>();
        }

        public override int GetWeight(int placeid, int transid)
        {
            return weights[(placeid, transid)];
        }

        public override IEnumerable<int> GetPlaceOutArcs(int placeId)
        {
            return placeOutArcs.TryGetValue(placeId, out var items) ? items : Enumerable.Empty<int>();
        }

        public override bool IsEmptyTransition(int transitionId)
        {
            return emptyTransitions.Contains(transitionId);
        }
    }
}
