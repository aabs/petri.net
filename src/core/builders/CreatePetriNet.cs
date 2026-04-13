using System.Diagnostics.Contracts;
using System.Text;
namespace petrinets2.core;

public class CreatePetriNet
{
    readonly object _sync = new();
    Dictionary<string, int> _placesByName;
    Dictionary<string, int> _transitionsByName;

    /// <summary>
    /// Adds the specs in textual shorthand
    /// </summary>
    /// <returns>a modified version of the graph with any transitions added</returns>
    /// <example>
    /// the examples below show the possible cases. Assume that place and transition names <i>must</i> be alphanumeric.
    /// <list type="table">
    /// <item>
    /// <term><c>t]-(p</c></term>
    /// <description>a simple links from a transition to a place</description>
    /// </item>
    /// <item><term><c>p)-[t</c></term>
    /// <description>a simple link from a place to a transition</description>
    /// </item>
    /// <item><term><c>p)-o[t</c></term>
    /// <description>An inhibiting link from a place to a transition</description>
    /// </item>

    /// <item><term><c>t]-2-(p</c></term>
    /// <description>a weighted link from a transition to a place (weight: 2)</description>
    /// </item>
    /// <item><term><c>p)-2-[t</c></term>
    /// <description>a weighted link from a place to a transition (weight: 2)</description>
    /// </item>
    /// <item><term><c>p)-2-o[t</c></term>
    /// <description>a weighted inhibition link from a place to a transition</description>
    /// </item>
    /// </list>
    /// </example>
    public static CreatePetriNet Parse(string spec)
    {
        Parser parser = new Parser(new Scanner(new MemoryStream(ASCIIEncoding.Default.GetBytes(spec))));
        parser.Parse();
        if (parser.errors.count > 0)
        {
            throw new ParserException(parser.errors);
        }
        return parser.Builder;
    }

    public CreatePetriNet GenerateArc(List<string> p,
                                                List<string> t,
                                                int weight,
                                                bool isInhibitor,
                                                bool isIntoTransition)
    {
        Contract.Requires(!p.Any(x => string.IsNullOrWhiteSpace(x)));
        Contract.Requires(!t.Any(x => string.IsNullOrWhiteSpace(x)));
        Contract.Requires(p.Intersect(t).Count() == 0);
        Contract.Requires(weight >= 1);
        var places = p.ToArray();
        var transitions = t.ToArray();
        var builder = WithPlaces(places).WithTransitions(transitions);
        foreach (var tran in t)
        {
            var connectionBuilder = builder.With(tran).Weight(weight);
            if (isInhibitor)
            {
                connectionBuilder.AsInhibitor();
            }
            if (isIntoTransition)
            {
                connectionBuilder.FedBy(places);
            }
            else
            {
                connectionBuilder.Feeding(places);
            }
            connectionBuilder.Done();
        }
        return builder;
    }

    public string Name { get; set; }
    public Dictionary<int, string> Places { get; set; }
    public Dictionary<string, int> PlaceMarkings { get; set; }
    public Dictionary<string, int> PlaceCapacities { get; set; }
    public Dictionary<int, string> Transitions { get; set; }
    public Dictionary<int, List<InArc>> InArcs { get; set; }
    public Dictionary<int, List<OutArc>> OutArcs { get; set; }
    public Dictionary<int, List<Action<GraphPetriNet>>> TransitionFunctions { get; set; }
    public CreatePetriNet(string name)
    {
        Contract.Requires(!string.IsNullOrWhiteSpace(name));
        Name = name;
        Places = new Dictionary<int, string>();
        _placesByName = new Dictionary<string, int>(StringComparer.Ordinal);
        PlaceMarkings = new Dictionary<string, int>();
        PlaceCapacities = new Dictionary<string, int>();
        Transitions = new Dictionary<int, string>();
        _transitionsByName = new Dictionary<string, int>(StringComparer.Ordinal);
        InArcs = new Dictionary<int, List<InArc>>();
        OutArcs = new Dictionary<int, List<OutArc>>();
        TransitionFunctions = new Dictionary<int, List<Action<GraphPetriNet>>>();
    }

    public static CreatePetriNet Called(string name)
    {
        var result = new CreatePetriNet(name);
        return result;
    }

    public CreatePetriNet WithPlaces(params string[] placeNames)
    {
        ArgumentNullException.ThrowIfNull(placeNames);
        if (placeNames.Length == 0)
        {
            return this;
        }

        lock (_sync)
        {
            EnsureReverseMapsSynchronized();
            var nextIndex = (Places.Count > 0 ? Places.Keys.Max() : -1) + 1;

            foreach (var placeName in placeNames)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(placeName);

                if (_placesByName.ContainsKey(placeName))
                {
                    continue;
                }

                Places[nextIndex] = placeName;
                _placesByName[placeName] = nextIndex;
                nextIndex++;
            }
        }

        return this;
    }
    public CreatePetriNet AndPlaces(params string[] placeNames) { return WithPlaces(placeNames); }
    public CreatePetriNet WithTransitions(params string[] transitionNames)
    {
        ArgumentNullException.ThrowIfNull(transitionNames);
        if (transitionNames.Length == 0)
        {
            return this;
        }

        lock (_sync)
        {
            EnsureReverseMapsSynchronized();
            var nextIndex = (Transitions.Count > 0 ? Transitions.Keys.Max() : -1) + 1;

            foreach (var transitionName in transitionNames)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(transitionName);

                if (_transitionsByName.ContainsKey(transitionName))
                {
                    continue;
                }

                Transitions[nextIndex] = transitionName;
                _transitionsByName[transitionName] = nextIndex;
                nextIndex++;
            }
        }

        return this;
    }
    public CreatePetriNet AndTransitions(params string[] transitionNames) { return WithTransitions(transitionNames); }
    public PetriNetConnectionBuilder With(string transitionName)
    {
        var result = new PetriNetConnectionBuilder(this, transitionName);
        return result;
    }

    public T CreateNet<T>() where T : class
    {
        T? result = null;
        if (typeof(T).Equals(typeof(GraphPetriNet)))
        {
            var tmp = new GraphPetriNet(
                Name,
                Places,
                Transitions,
                InArcs,
                OutArcs);
            foreach (var capacity in PlaceCapacities)
            {
                tmp.PlaceCapacities[PlaceIndex(capacity.Key)] = capacity.Value;
            }
            result = tmp as T;
        }
        if (typeof(T).Equals(typeof(MatrixPetriNet)))
        {
            result = new MatrixPetriNet(
                Name,
                Places,
                Transitions,
                InArcs,
                OutArcs) as T;
        }
        if (result == null)
        {
            throw new ApplicationException("Unrecognised petri net type requested");
        }
        return result;
    }

    public Marking CreateMarking()
    {
        var result = new Marking(Places.Count);
        foreach (var marking in PlaceMarkings)
        {
            if (Places.ContainsValue(marking.Key))
            {
                result[PlaceIndex(marking.Key)] = marking.Value;
            }
        }
        return result;
    }

    public PetriNetEventBuilder WhenFiring(string transitionName)
    {
        var result = new PetriNetEventBuilder(this, transitionName);
        return result;
    }

    public int TransitionIndex(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_sync)
        {
            EnsureReverseMapsSynchronized();
            if (_transitionsByName.TryGetValue(name, out var index))
            {
                return index;
            }
        }

        throw new KeyNotFoundException($"Transition '{name}' is not registered in builder '{Name}'.");
    }

    public int PlaceIndex(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_sync)
        {
            EnsureReverseMapsSynchronized();
            if (_placesByName.TryGetValue(name, out var index))
            {
                return index;
            }
        }

        throw new KeyNotFoundException($"Place '{name}' is not registered in builder '{Name}'.");
    }
    public void AddInArc(string placeName,
                         string transitionName,
                         bool isInhibitor,
                         int weight)
    {
        lock (_sync)
        {
            EnsureReverseMapsSynchronized();
            if (InArcs == null)
            {
                InArcs = new Dictionary<int, List<InArc>>();
            }

            var placeIndex = PlaceIndex(placeName);
            var transitionIndex = TransitionIndex(transitionName);

            if (!InArcs.ContainsKey(transitionIndex))
            {
                InArcs[transitionIndex] = new List<InArc>();
            }
            InArcs[transitionIndex].Add(new InArc(placeIndex, weight, isInhibitor));
        }
    }

    public void AddOutArc(string transitionName, string placeName,
                         int weight = 1)
    {
        lock (_sync)
        {
            EnsureReverseMapsSynchronized();
            if (OutArcs == null)
            {
                OutArcs = new Dictionary<int, List<OutArc>>();
            }

            var fromIndex = TransitionIndex(transitionName);
            var toIndex = PlaceIndex(placeName);
            if (!OutArcs.ContainsKey(fromIndex))
            {
                OutArcs[fromIndex] = new List<OutArc>();
            }
            OutArcs[fromIndex].Add(new OutArc(toIndex, weight));
        }
    }

    public void AddEvent(string transitionName, Action<GraphPetriNet> task)
    {
        ArgumentNullException.ThrowIfNull(task);

        lock (_sync)
        {
            var transition = TransitionIndex(transitionName);
            if (TransitionFunctions == null)
            {
                TransitionFunctions = new Dictionary<int, List<Action<GraphPetriNet>>>();
            }
            if (!TransitionFunctions.ContainsKey(transition))
            {
                TransitionFunctions[transition] = new List<Action<GraphPetriNet>>();
            }
            TransitionFunctions[transition].Add(task);
        }
    }
    public PlaceSpecifier WithPlace(string placeName)
    {
        Contract.Requires(!string.IsNullOrWhiteSpace(placeName));
        return new PlaceSpecifier(this, placeName);
    }

    void EnsureReverseMapsSynchronized()
    {
        if (Places == null)
        {
            Places = new Dictionary<int, string>();
        }

        if (Transitions == null)
        {
            Transitions = new Dictionary<int, string>();
        }

        if (_placesByName == null || _placesByName.Count != Places.Count || Places.Any(pair => !_placesByName.TryGetValue(pair.Value, out var index) || index != pair.Key))
        {
            _placesByName = BuildReverseMap(Places);
        }

        if (_transitionsByName == null || _transitionsByName.Count != Transitions.Count || Transitions.Any(pair => !_transitionsByName.TryGetValue(pair.Value, out var index) || index != pair.Key))
        {
            _transitionsByName = BuildReverseMap(Transitions);
        }
    }

    static Dictionary<string, int> BuildReverseMap(Dictionary<int, string> forward)
    {
        var result = new Dictionary<string, int>(forward.Count, StringComparer.Ordinal);
        foreach (var pair in forward.OrderBy(pair => pair.Key))
        {
            if (!result.ContainsKey(pair.Value))
            {
                result[pair.Value] = pair.Key;
            }
        }

        return result;
    }
}