namespace petrinets2.core;

public sealed class PetriNetBuilder
{
    readonly Dictionary<string, int> placeIndices = new(StringComparer.Ordinal);
    readonly Dictionary<string, int> transitionIndices = new(StringComparer.Ordinal);
    readonly List<string> placeNames = new();
    readonly List<string> transitionNames = new();
    readonly Dictionary<int, List<InArcSpec>> inArcsByTransition = new();
    readonly Dictionary<int, List<OutArcSpec>> outArcsByTransition = new();
    readonly Dictionary<int, int> initialMarkingsByPlace = new();
    readonly Dictionary<int, int> capacitiesByPlace = new();

    public string Name { get; private set; } = "unnamed";

    public PetriNetBuilder WithName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        return this;
    }

    public PetriNetBuilder WithPlaces(params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);

        for (var i = 0; i < names.Length; i++)
        {
            AddingPlace(names[i]);
        }

        return this;
    }

    public PetriNetBuilder WithTransitions(params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);

        for (var i = 0; i < names.Length; i++)
        {
            AddingTransition(names[i]);
        }

        return this;
    }

    public PetriNetBuilder AddingPlace(string placeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placeName);

        if (!placeIndices.ContainsKey(placeName))
        {
            placeIndices[placeName] = placeNames.Count;
            placeNames.Add(placeName);
        }

        return this;
    }

    public PetriNetBuilder AddingTransition(string transitionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transitionName);

        if (!transitionIndices.ContainsKey(transitionName))
        {
            transitionIndices[transitionName] = transitionNames.Count;
            transitionNames.Add(transitionName);
        }

        return this;
    }

    public PlaceBuilder WithPlace(string placeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placeName);
        var placeIndex = PlaceIndex(placeName);
        return new PlaceBuilder(this, placeIndex);
    }

    public PetriNetBuilder WithInitialMarking(string placeName, int tokens)
    {
        if (tokens < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tokens), "Initial marking must be non-negative.");
        }

        initialMarkingsByPlace[PlaceIndex(placeName)] = tokens;
        return this;
    }

    public PetriNetBuilder WithCapacity(string placeName, int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be non-negative.");
        }

        capacitiesByPlace[PlaceIndex(placeName)] = capacity;
        return this;
    }

    public PetriNetBuilder AddingInputArc(string sourcePlace, string targetTransition, int weight = 1, bool isInhibitor = false)
    {
        ValidateWeight(weight);
        var placeIndex = PlaceIndex(sourcePlace);
        var transitionIndex = TransitionIndex(targetTransition);

        if (!inArcsByTransition.TryGetValue(transitionIndex, out var arcs))
        {
            arcs = new List<InArcSpec>();
            inArcsByTransition[transitionIndex] = arcs;
        }

        arcs.Add(new InArcSpec(placeIndex, weight, isInhibitor));
        return this;
    }

    public PetriNetBuilder AddingOutputArc(string sourceTransition, string targetPlace, int weight = 1)
    {
        ValidateWeight(weight);
        var transitionIndex = TransitionIndex(sourceTransition);
        var placeIndex = PlaceIndex(targetPlace);

        if (!outArcsByTransition.TryGetValue(transitionIndex, out var arcs))
        {
            arcs = new List<OutArcSpec>();
            outArcsByTransition[transitionIndex] = arcs;
        }

        arcs.Add(new OutArcSpec(placeIndex, weight));
        return this;
    }

    public PetriNetBuilder AddingArc(string source, string target, int weight = 1, bool isInhibitor = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);

        var sourceIsPlace = placeIndices.ContainsKey(source);
        var sourceIsTransition = transitionIndices.ContainsKey(source);
        var targetIsPlace = placeIndices.ContainsKey(target);
        var targetIsTransition = transitionIndices.ContainsKey(target);

        if (sourceIsPlace && targetIsTransition)
        {
            return AddingInputArc(source, target, weight, isInhibitor);
        }

        if (sourceIsTransition && targetIsPlace)
        {
            return AddingOutputArc(source, target, weight);
        }

        throw new InvalidOperationException($"Arc '{source}' -> '{target}' does not connect place-to-transition or transition-to-place.");
    }

    public GraphPetriNet BuildGraph()
    {
        var places = ToDensePlaceMap();
        var transitions = ToDenseTransitionMap();
        var inArcs = ToDenseInArcs();
        var outArcs = ToDenseOutArcs();

        var graph = new GraphPetriNet(Name, places, transitions, inArcs, outArcs);

        foreach (var capacity in capacitiesByPlace)
        {
            graph.PlaceCapacities[capacity.Key] = capacity.Value;
        }

        return graph;
    }

    public MatrixPetriNet BuildMatrix()
    {
        return new MatrixPetriNet(Name, ToDensePlaceMap(), ToDenseTransitionMap(), ToDenseInArcs(), ToDenseOutArcs());
    }

    public Marking BuildInitialMarking()
    {
        return initialMarkingsByPlace.Count == 0
            ? new Marking(placeNames.Count)
            : new Marking(placeNames.Count, initialMarkingsByPlace);
    }

    public bool ContainsPlace(string placeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placeName);
        return placeIndices.ContainsKey(placeName);
    }

    public bool ContainsTransition(string transitionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transitionName);
        return transitionIndices.ContainsKey(transitionName);
    }

    public int PlaceIndex(string placeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placeName);

        if (placeIndices.TryGetValue(placeName, out var placeIndex))
        {
            return placeIndex;
        }

        throw new KeyNotFoundException($"Place '{placeName}' is not registered in builder '{Name}'.");
    }

    public int TransitionIndex(string transitionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transitionName);

        if (transitionIndices.TryGetValue(transitionName, out var transitionIndex))
        {
            return transitionIndex;
        }

        throw new KeyNotFoundException($"Transition '{transitionName}' is not registered in builder '{Name}'.");
    }

    Dictionary<int, string> ToDensePlaceMap()
    {
        var result = new Dictionary<int, string>(placeNames.Count);
        for (var i = 0; i < placeNames.Count; i++)
        {
            result[i] = placeNames[i];
        }

        return result;
    }

    Dictionary<int, string> ToDenseTransitionMap()
    {
        var result = new Dictionary<int, string>(transitionNames.Count);
        for (var i = 0; i < transitionNames.Count; i++)
        {
            result[i] = transitionNames[i];
        }

        return result;
    }

    Dictionary<int, List<InArc>> ToDenseInArcs()
    {
        var result = new Dictionary<int, List<InArc>>(transitionNames.Count);

        for (var transitionIndex = 0; transitionIndex < transitionNames.Count; transitionIndex++)
        {
            if (!inArcsByTransition.TryGetValue(transitionIndex, out var arcs))
            {
                result[transitionIndex] = new List<InArc>();
                continue;
            }

            var values = new List<InArc>(arcs.Count);
            for (var i = 0; i < arcs.Count; i++)
            {
                var arc = arcs[i];
                values.Add(new InArc(arc.SourcePlaceIndex, arc.Weight, arc.IsInhibitor));
            }

            result[transitionIndex] = values;
        }

        return result;
    }

    Dictionary<int, List<OutArc>> ToDenseOutArcs()
    {
        var result = new Dictionary<int, List<OutArc>>(transitionNames.Count);

        for (var transitionIndex = 0; transitionIndex < transitionNames.Count; transitionIndex++)
        {
            if (!outArcsByTransition.TryGetValue(transitionIndex, out var arcs))
            {
                result[transitionIndex] = new List<OutArc>();
                continue;
            }

            var values = new List<OutArc>(arcs.Count);
            for (var i = 0; i < arcs.Count; i++)
            {
                var arc = arcs[i];
                values.Add(new OutArc(arc.TargetPlaceIndex, arc.Weight));
            }

            result[transitionIndex] = values;
        }

        return result;
    }

    static void ValidateWeight(int weight)
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than zero.");
        }
    }

    public sealed class PlaceBuilder
    {
        readonly PetriNetBuilder builder;
        readonly int placeIndex;

        internal PlaceBuilder(PetriNetBuilder builder, int placeIndex)
        {
            this.builder = builder;
            this.placeIndex = placeIndex;
        }

        public PlaceBuilder WithInitialMarking(int tokens)
        {
            if (tokens < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tokens), "Initial marking must be non-negative.");
            }

            builder.initialMarkingsByPlace[placeIndex] = tokens;
            return this;
        }

        public PlaceBuilder WithCapacity(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be non-negative.");
            }

            builder.capacitiesByPlace[placeIndex] = capacity;
            return this;
        }

        public PetriNetBuilder Done()
        {
            return builder;
        }
    }

    readonly record struct InArcSpec(int SourcePlaceIndex, int Weight, bool IsInhibitor);
    readonly record struct OutArcSpec(int TargetPlaceIndex, int Weight);
}
