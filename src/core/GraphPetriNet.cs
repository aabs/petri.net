namespace petrinets2.core;

public class GraphPetriNet : PetriNetBase
{
    #region ctors

    public GraphPetriNet(string id,
                    Dictionary<int, string> placeNames,
                    Dictionary<int, string> transitionNames,
                    Dictionary<int, List<InArc>> inArcs,
                    Dictionary<int, List<OutArc>> outArcs
        )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(placeNames);
        ArgumentNullException.ThrowIfNull(transitionNames);
        ArgumentNullException.ThrowIfNull(inArcs);
        ArgumentNullException.ThrowIfNull(outArcs);

        if (placeNames.Values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Place names must be non-empty.", nameof(placeNames));
        }
        if (transitionNames.Values.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Transition names must be non-empty.", nameof(transitionNames));
        }

        foreach (var transition in inArcs)
        {
            if (!transitionNames.ContainsKey(transition.Key))
            {
                throw new ArgumentException("InArcs contains an unknown transition id.", nameof(inArcs));
            }

            foreach (var arc in transition.Value)
            {
                if (!placeNames.ContainsKey(arc.Source))
                {
                    throw new ArgumentException("InArcs contains an unknown place id.", nameof(inArcs));
                }
            }
        }

        foreach (var transition in outArcs)
        {
            if (!transitionNames.ContainsKey(transition.Key))
            {
                throw new ArgumentException("OutArcs contains an unknown transition id.", nameof(outArcs));
            }

            foreach (var arc in transition.Value)
            {
                if (!placeNames.ContainsKey(arc.Target))
                {
                    throw new ArgumentException("OutArcs contains an unknown place id.", nameof(outArcs));
                }
            }
        }

        Id = id;
        Places = new Dictionary<int, string>(placeNames);
        Transitions = new Dictionary<int, string>(transitionNames);
        InArcs = inArcs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());

        // each arc into a transition can be seen as an arc out of a place 
        // (which may be convenient for conflict resolution)
        foreach (var transitionInArcs in InArcs)
        {
            foreach (var inArc in transitionInArcs.Value)
            {
                if (!PlaceOutArcs.ContainsKey(inArc.Source))
                {
                    PlaceOutArcs[inArc.Source] = new List<OutArc>();
                }
                PlaceOutArcs[inArc.Source].Add(new OutArc(transitionInArcs.Key, inArc.Weight));
            }
        }

        OutArcs = outArcs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
        PlaceCapacities = new Dictionary<int, int>();
    }

    public GraphPetriNet(string id,
            Dictionary<int, string> placeNames,
            Dictionary<int, string> transitionNames,
            Dictionary<int, List<InArc>> inArcs,
            Dictionary<int, List<OutArc>> outArcs,
            Dictionary<int, int> transitionOrdering)
        : this(id, placeNames, transitionNames, inArcs, outArcs)
    {
        ArgumentNullException.ThrowIfNull(transitionOrdering);

        var x = transitionNames.Select(t => t.Key).Except(transitionOrdering.Select(t => t.Key));
        var y = transitionOrdering.Union(x.ToDictionary(t => t, t => 0)); // baseline priority level
        TransitionPriorities = y.ToDictionary(a => a.Key, a => a.Value);
    }
    #endregion

    #region graph model and state data
    public string Id { get; set; }

    public const int MaxSize = 1000;

    public Dictionary<int, string> Places = new Dictionary<int, string>();
    public Dictionary<int, int> PlaceCapacities = new Dictionary<int, int>();
    public Dictionary<int, string> Transitions = new Dictionary<int, string>();

    //        public Dictionary<int, int> Markings = new Dictionary<int, int>();
    public Dictionary<int, List<InArc>> InArcs = new Dictionary<int, List<InArc>>();
    public Dictionary<int, List<OutArc>> OutArcs = new Dictionary<int, List<OutArc>>();
    public Dictionary<int, List<OutArc>> PlaceOutArcs = new Dictionary<int, List<OutArc>>();
    public Dictionary<int, int> TransitionPriorities = new Dictionary<int, int>();
    public Dictionary<int, List<Action<int>>> TransitionFunctions = new Dictionary<int, List<Action<int>>>();
    #endregion

    #region graph construction
    public void AddArcFromTransition(int placeId, int transitionId, int weight = 1)
    {
        ValidateWeight(weight);

        if (!OutArcs.ContainsKey(transitionId))
        {
            OutArcs[transitionId] = new List<OutArc>();
        }

        OutArcs[transitionId].Add(new OutArc(placeId, weight));
    }

    public void AddArcIntoTransition(int placeId, int transitionId, int weight = 1, bool isInhibitor = false)
    {
        ValidateWeight(weight);

        if (!InArcs.ContainsKey(transitionId))
        {
            InArcs[transitionId] = new List<InArc>();
        }

        InArcs[transitionId].Add(new InArc(placeId, weight, isInhibitor));

        if (!PlaceOutArcs.ContainsKey(placeId))
        {
            PlaceOutArcs[placeId] = new List<OutArc>();
        }
        PlaceOutArcs[placeId].Add(new OutArc(transitionId, weight));
    }

    public void AddArcFromPlace(int placeId, int transitionId, int weight = 1)
    {
        ValidateWeight(weight);

        if (!PlaceOutArcs.ContainsKey(placeId))
        {
            PlaceOutArcs[placeId] = new List<OutArc>();
        }

        PlaceOutArcs[placeId].Add(new OutArc(transitionId, weight));
    }

    public void AddLinearTransition(int placeIn, int placeOut, int transitionId)
    {
        AddArcFromPlace(placeIn, transitionId);
        AddArcIntoTransition(placeIn, transitionId);
        AddArcFromTransition(placeOut, transitionId);
    }

    public void CreateTransitions(IEnumerable<Tuple<int, int, int>> transitions)
    {
        foreach (var t in transitions)
        {
            AddLinearTransition(t.Item1, t.Item2, t.Item3);
        }
    }

    #endregion

    #region accessors
    public override IEnumerable<int> InhibitorsIntoTransition(int transitionId)
    {
        if (InArcs.ContainsKey(transitionId))
        {
            return from a in InArcs[transitionId]
                   where a.IsInhibitor
                   select a.Source;
        }
        return new int[] { };
    }

    internal IEnumerable<InArc> GetInArcs(int transitionId)
    {
        if (InArcs.TryGetValue(transitionId, out var result))
        {
            return result;
        }
        return new InArc[] { };
    }

    internal IEnumerable<OutArc> GetOutArcs(int transitionId)
    {
        if (!OutArcs.ContainsKey(transitionId))
            return new OutArc[] { };
        return OutArcs[transitionId];
    }

    internal IEnumerable<Action<int>> GetTransitionFunctions(int transitionId)
    {
        if (!TransitionFunctions.ContainsKey(transitionId))
            return new Action<int>[] { };
        return TransitionFunctions[transitionId];
    }

    public override IEnumerable<int> NonInhibitorsIntoTransition(int transitionId)
    {
        if (!InArcs.ContainsKey(transitionId))
        {
            return new int[] { };
        }
        return GetInArcs(transitionId).Where(x => !x.IsInhibitor).Select(x => x.Source);
    }

    public IEnumerable<int> AllEnabledTransitions(Marking m)
    {
        return (from t in Transitions
                where IsEnabled(t.Key, m)
                select t.Key);
    }

    internal List<OutArc> AllSourcePlaces()
    {
        List<OutArc> result = new List<OutArc>();
        foreach (var p in OutArcs)
        {
            result.AddRange(OutArcs[p.Key]);
        }
        return result;
    }

    internal List<InArc> AllDestinationPlaces()
    {
        List<InArc> result = new List<InArc>();
        foreach (var p in InArcs)
        {
            result.AddRange(InArcs[p.Key]);
        }
        return result;
    }

    public override IEnumerable<int> AllPlaces()
    {
        return Places.Keys;
        /*            var result = AllSourcePlaces()
                        .Select(x => x.Target)
                        .Concat(AllDestinationPlaces()
                                    .Select(y => y.Source)).ToList();
                    return result;*/
    }

    internal IEnumerable<int> AllMarkedPlaces(Marking m)
    {
        for (int i = 0; i < m.Size; i++)
        {
            if (m[i] > 0)
            {
                yield return i;
            }
        }
    }

    internal IEnumerable<int> PlacesFeedingIntoTransitions()
    {
        var result = (from p in PlaceOutArcs select p.Key).ToList();
        return result;
    }



    public IEnumerable<int> GetConflictedPlaces(Marking m)
    {
        var q = from p in AllPlaces()
                where PlaceIsConflicted(p, m)
                select p;
        return q;
    }

    private IEnumerable<int> SharedInputPlaces(int t1, int t2)
    {
        return GetInArcs(t1)
                .Select(ia => ia.Source)
                .Intersect(GetInArcs(t2).Select(ia => ia.Source));
    }
    #endregion

    #region extensibility
    public void RegisterFunction(int transitionId, Action<int> fn)
    {
        if (!TransitionFunctions.ContainsKey(transitionId))
        {
            TransitionFunctions[transitionId] = new List<Action<int>>();
        }
        TransitionFunctions[transitionId].Add(fn);
    }
    #endregion

    #region net execution

    public int? GetNextTransitionToFire(Marking m)
    {
        var ets = AllEnabledTransitions(m);
        if (ets.Count()< 1)
        {
            return null;
        }
        return (from t in ets
                orderby GetTransitionPriority(t) descending
                select t).First();
    }

    public override bool IsEmptyTransition(int transitionId)
    {
        return (!InArcs.ContainsKey(transitionId) || InArcs[transitionId].Count == 0);
    }

    /// <summary>
    /// invokes the first enabled transition in the petri net under the supplied <see cref="Marking"/>.
    /// </summary>
    /// <param name="m">The marking under which transition activation is calculated.</param>
    /// <returns>A new <see cref="Marking"/> containing the result of transition firing on the marking.</returns>
    /// <remarks>
    /// This method will not have any side effects on the <see cref="Marking"/> passed into the function or on the net itself.
    /// 
    /// This method works by choosing the enabled transition with the highest priority.
    /// </remarks>
    public virtual Marking Fire(Marking m)
    {
        ArgumentNullException.ThrowIfNull(m);

        var result = new Marking(m);
        int? transitionId = GetNextTransitionToFire(m);
        
        if (!transitionId.HasValue)
            return result;

        int tran = transitionId.Value;

        foreach (var place in GetInArcs(tran).Where(x => x.IsInhibitor == false))
            result[place.Source] = m[place.Source] - GetWeight(place.Source, tran);

        foreach (var arc in GetOutArcs(tran))
            result[arc.Target] = result[arc.Target] + arc.Weight;

        if (TransitionFunctions.ContainsKey(tran))
            TransitionFunctions[tran].ForEach(a => a(tran));

        return result;
    }
    #endregion

#if USING_CONTRACTS
    [ContractInvariantMethod]
    protected void ObjectInvariant()
    {
        Contract.Invariant(!string.IsNullOrEmpty(Id), "No empty petri net ID");
        Contract.Invariant(Places != null, "no null places");
        Contract.Invariant(Transitions != null, "no null transitions");
        Contract.Invariant(InArcs != null, "no null InArcs");
        Contract.Invariant(OutArcs != null, "no null OutArcs");
        Contract.Invariant(PlaceOutArcs != null, "no null PlaceOutArcs");
        Contract.Invariant(TransitionFunctions != null, "no null TransitionFunctions");
    }
#endif


    public int GetTransitionPriority(int t)
    {
        return TransitionPriorities.ContainsKey(t) ? TransitionPriorities[t] : 0;
    }
    public override IEnumerable<int> GetPlaceOutArcs(int placeId)
    {
        if (PlaceOutArcs.ContainsKey(placeId))
        {
            return PlaceOutArcs[placeId].Select(x => x.Target);
        }
        return new int[] { };
    }
    public override int GetWeight(int placeid, int transid)
    {
        if (!PlaceOutArcs.TryGetValue(placeid, out var outArcs))
        {
            throw new KeyNotFoundException($"Place '{placeid}' has no outgoing arcs.");
        }
        var outArc = outArcs.FirstOrDefault(x => x.Target == transid);
        if (outArc != null)
        {
            return outArc.Weight;
        }
        throw new KeyNotFoundException($"No arc exists from place '{placeid}' to transition '{transid}'.");
    }

    static void ValidateWeight(int weight)
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than zero.");
        }
    }
}

public class ConflictSet
{
    public IEnumerable<int> ConflictedTransitions { get; set; } = Array.Empty<int>();
    public IEnumerable<int> ContestedPlaces { get; set; } = Array.Empty<int>();
}