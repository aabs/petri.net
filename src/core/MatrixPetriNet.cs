using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Diagnostics.Contracts;

namespace petrinets2.core;

/// <summary>
/// Description of MatrixPetriNet.
/// </summary>
public class MatrixPetriNet : PetriNetBase
{
    #region ctors
    /// <summary>
    /// Create a new Petri Net using Sparse Matrices for arc representation
    /// </summary>
    /// <param name="id">The name of the Petri net</param>
    /// <param name="placeNames">A complete list of the names of the places</param>
    /// <param name="markings">A mapping between the places and integers. The initial marking of the nett</param>
    /// <param name="transitionNames">A zero based contiguous sequence of names for each of the transitions in the net</param>
    /// <param name="inArcs">Arcs coming into transitions</param>
    /// <param name="outArcs">Arcs from transitions into places</param>
    /// <remarks>
    /// <see cref="placeNames"/> and <see cref="transitionNames"/> must contain a contiguous sequence of
    /// identifier for all of the places and transitions. These values are used to calculate
    /// what the dimensions of the matrices are, so the keys of the dictionaries must be zero based and
    /// contiguous.
    /// </remarks>
    [ContractVerification(false)]
    public MatrixPetriNet(string id,
        Dictionary<int, string> placeNames,
        Dictionary<int, string> transitionNames,
        Dictionary<int, List<InArc>> inArcs,
        Dictionary<int, List<OutArc>> outArcs,
        Dictionary<int, int> transitionOrdering)
        : this(id, placeNames, transitionNames, inArcs, outArcs)
    {
        ArgumentNullException.ThrowIfNull(transitionNames);
        ArgumentNullException.ThrowIfNull(transitionOrdering);

        var x = transitionNames.Select(t => t.Key).Except(transitionOrdering.Select(t => t.Key));
        var y = transitionOrdering.Union(x.ToDictionary(t => t, t => 0)); // baseline priority level
        TransitionPriorities = y.ToDictionary(a => a.Key, a => a.Value);
    }

    /// <summary>
    /// Create a new Petri Net using Sparse Matrices for arc representation
    /// </summary>
    /// <param name="id">The name of the Petri net</param>
    /// <param name="placeNames">A complete list of the names of the places</param>
    /// <param name="markings">A mapping between the places and integers. The initial marking of the nett</param>
    /// <param name="transitionNames">A zero based contiguous sequence of names for each of the transitions in the net</param>
    /// <param name="inArcs">Arcs coming into transitions</param>
    /// <param name="outArcs">Arcs from transitions into places</param>
    /// <remarks>
    /// <see cref="placeNames"/> and <see cref="transitionNames"/> must contain a contiguous sequence of
    /// identifier for all of the places and transitions. These values are used to calculate
    /// what the dimensions of the matrices are, so the keys of the dictionaries must be zero based and
    /// contiguous.
    /// </remarks>
    [ContractVerification(false)]
    public MatrixPetriNet(string id,
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

        Contract.Requires(!string.IsNullOrEmpty(id), "must provide valid PN ID");
        Contract.Requires(placeNames != null, "must provide a set of place names");
        Contract.Requires(transitionNames != null, "must provide a set of transition names");
        Contract.Requires(inArcs != null, "inArcs cannot be null");
        Contract.Requires(outArcs != null, "outArcs cannot be null");
        /*Contract.Requires(placeNames.All(x => (x.Key >= 0) && (x.Key <= placeNames.Count)), "all place names must be identifiable");
        Contract.Requires(markings.All(x => (x.Key >= 0) && (x.Key <= placeNames.Count)), "all markings must be for known place names");
        Contract.Requires(transitionNames.All(x => (x.Key >= 0) && (x.Key <= transitionNames.Count)), "all transitions must be identifiable");
        Contract.Requires(inArcs.All(x => (x.Key >= 0) && (x.Key <= transitionNames.Count)), "all in arcs must refer to known transitions");
        Contract.Requires(outArcs.All(x => (x.Key >= 0) && (x.Key <= transitionNames.Count)), "all out arcs must refer to known transitions");
        */
        Contract.Ensures(Transitions.Count == transitionNames!.Count);
        Contract.Ensures(Places.Count == placeNames!.Count);

        Id = id;
        var tmpMatrix = new SparseMatrix(placeNames.Count, transitionNames.Count);
        InMatrix = tmpMatrix;
        OutMatrix = new SparseMatrix(placeNames.Count, transitionNames.Count);
        Places = placeNames;
        Transitions = transitionNames;
        foreach (var transitionInArcs in inArcs!)
        {
            if (transitionInArcs.Value is null)
                continue;

            foreach (var inArc in transitionInArcs.Value)
            {
                InMatrix[inArc.Source, transitionInArcs.Key] = inArc.IsInhibitor ? double.NaN : inArc.Weight;
            }
        }

        foreach (var transitionOutArcs in outArcs!)
        {
            if (transitionOutArcs.Value is null)
                continue;

            foreach (var outArc in transitionOutArcs.Value)
            {
                OutMatrix[outArc.Target, transitionOutArcs.Key] = outArc.Weight;
            }
        }

        BuildSparseConnectivityIndexes(inArcs, outArcs);
    }

    #endregion

    #region graph model and state data
    public string Id { get; set; }

    /*
     * A note on how the matrices are constructed
     * 
     * Rows are for places and Columns are for transitions
     * therefore M[i,j] is the adjacency of place i to transition j.
     * M.GetRow(i) gets the adjacencies for place i and
     * M.GetColumn(j) gets the adjacencies for transition j.
     */
    public SparseMatrix InMatrix { get; set; }
    public SparseMatrix OutMatrix { get; set; }
    public SparseMatrix FlowMatrix { get; set; } = null!;
    readonly Dictionary<int, HashSet<int>> nonInhibitorPlacesByTransition = new();
    readonly Dictionary<int, HashSet<int>> inhibitorPlacesByTransition = new();
    readonly Dictionary<int, HashSet<int>> outputPlacesByTransition = new();
    readonly Dictionary<int, HashSet<int>> effectiveSupportByTransition = new();
    readonly Dictionary<int, HashSet<int>> placeOutTransitions = new();
    public Dictionary<int, int> TransitionPriorities = new Dictionary<int, int>();
    public Dictionary<int, string> Places = new Dictionary<int, string>();
    public Dictionary<int, string> Transitions = new Dictionary<int, string>();
    public Dictionary<int, List<Action<int>>> TransitionFunctions = new Dictionary<int, List<Action<int>>>();
    #endregion

    #region graph construction
    public void AddArcFromTransition(int placeId, int transitionId)
    {
        Contract.Requires(Places.ContainsKey(placeId));
        Contract.Requires(Transitions.ContainsKey(transitionId));
        Contract.Ensures(OutMatrix[placeId, transitionId] == Contract.OldValue(OutMatrix[placeId, transitionId]) + 1);

        OutMatrix[placeId, transitionId]++;
        AddToIndex(outputPlacesByTransition, transitionId, placeId);
        AddToIndex(effectiveSupportByTransition, transitionId, placeId);
    }

    public void AddArcIntoTransition(int placeId, int transitionId)
    {
        Contract.Requires(Places.ContainsKey(placeId));
        Contract.Requires(Transitions.ContainsKey(transitionId));
        Contract.Ensures(InMatrix[placeId, transitionId] == Contract.OldValue(InMatrix[placeId, transitionId]) + 1);

        InMatrix[placeId, transitionId]++;
        AddToIndex(nonInhibitorPlacesByTransition, transitionId, placeId);
        AddToIndex(placeOutTransitions, placeId, transitionId);
        AddToIndex(effectiveSupportByTransition, transitionId, placeId);
    }

    #endregion

    #region accessors
    public override int GetWeight(int placeid, int transid)
    {
        return (int)InMatrix[placeid, transid];
    }

    //[Pure]
    internal bool ArcIsInhibitor(int placeId, int transitionId)
    {
        Contract.Requires(Places.ContainsKey(placeId));
        Contract.Requires(Transitions.ContainsKey(transitionId));

        return double.IsNaN(InMatrix[placeId, transitionId]);
    }

    //[Pure]
    public override IEnumerable<int> InhibitorsIntoTransition(int transitionId)
    {
        Contract.Requires(Transitions.ContainsKey(transitionId));

        if (!inhibitorPlacesByTransition.TryGetValue(transitionId, out var inhibitorPlaces))
        {
            yield break;
        }

        foreach (var placeId in inhibitorPlaces)
        {
            yield return placeId;
        }
    }

    //[Pure]
    public override IEnumerable<int> NonInhibitorsIntoTransition(int transitionId)
    {
        Contract.Requires(Transitions.ContainsKey(transitionId));

        if (!nonInhibitorPlacesByTransition.TryGetValue(transitionId, out var inputPlaces))
        {
            yield break;
        }

        foreach (var placeId in inputPlaces)
        {
            yield return placeId;
        }
    }
    #endregion

    #region extensibility
    public void RegisterFunction(int transitionId, Action<int> fn)
    {
        var safeFn = fn ?? throw new ArgumentNullException(nameof(fn));

        Contract.Requires(Transitions.ContainsKey(transitionId));
        Contract.Requires(safeFn != null);
        Contract.Ensures(TransitionFunctions.ContainsKey(transitionId));

        if (!TransitionFunctions.ContainsKey(transitionId))
        {
            TransitionFunctions[transitionId] = new List<Action<int>>();
        }
        TransitionFunctions[transitionId].Add(safeFn!);
    }
    #endregion

    #region net execution
    bool AllInhibitorsAreFromEmptyPlaces(int transitionId, Marking m)
    {
        Contract.Requires(Transitions.ContainsKey(transitionId));

        if (!inhibitorPlacesByTransition.TryGetValue(transitionId, out var inhibitorPlaces))
        {
            return true;
        }

        foreach (var placeId in inhibitorPlaces)
        {
            if (m[placeId] != 0)
            {
                return false;
            }
        }

        return true;
    }

    bool AllInArcPlacesHaveMoreTokensThanTheArcWeight(int transitionId, Marking m)
    {
        Contract.Requires(Transitions.ContainsKey(transitionId));

        if (!nonInhibitorPlacesByTransition.TryGetValue(transitionId, out var inputPlaces))
        {
            return true;
        }

        foreach (var placeId in inputPlaces)
        {
            var inputWeight = InMatrix[placeId, transitionId];
            if (m[placeId] < inputWeight)
            {
                return false;
            }
        }

        return true;
    }

    public override bool IsEmptyTransition(int transitionId)
    {
        Contract.Requires(Transitions.ContainsKey(transitionId));

        var hasInputs = nonInhibitorPlacesByTransition.TryGetValue(transitionId, out var inputPlaces)
                        && inputPlaces.Count > 0;
        var hasInhibitors = inhibitorPlacesByTransition.TryGetValue(transitionId, out var inhibitorPlaces)
                            && inhibitorPlaces.Count > 0;

        return !hasInputs && !hasInhibitors;
    }

    public IEnumerable<int> GetEnabledTransitions(Marking m)
    {
        // subtract the weights from the markings then sum each element in the resulting vectir
        // if the result is greater than or equal to zero then the transition is enabled
        for (int i = 0; i < OutMatrix.ColumnCount; i++)
        {
            if (IsEnabled(i, m))
                yield return i;
        }
    }

    public FiringPlan CreateFiringPlan(Marking m)
    {
        ArgumentNullException.ThrowIfNull(m);

        return BuildFiringPlan(GetEnabledTransitions(m), IsConflicted(m), GetTransitionPriority);
    }

    public int? GetNextTransitionToFire(Marking m)
    {
        ArgumentNullException.ThrowIfNull(m);

        return TransitionSelection.SelectHighestPriority(GetEnabledTransitions(m), GetTransitionPriority);
    }

    public virtual Marking Fire(Marking m)
    {
        ArgumentNullException.ThrowIfNull(m);

        var result = new Marking(m);
        var firingPlan = CreateFiringPlan(m);

        if (firingPlan.IsEmpty)
        {
            return result;
        }

        using var deltaLease = ScratchBufferPooling.AcquireDeltaScratch();
        try
        {
            var deltasByPlace = deltaLease.Buffer;
            foreach (var transitionId in firingPlan.TransitionIds)
            {
                if (outputPlacesByTransition.TryGetValue(transitionId, out var outputPlaces))
                {
                    foreach (var placeId in outputPlaces)
                    {
                        AddDelta(deltasByPlace, placeId, (int)OutMatrix[placeId, transitionId]);
                    }
                }

                if (!nonInhibitorPlacesByTransition.TryGetValue(transitionId, out var inputPlaces))
                {
                    continue;
                }

                foreach (var placeId in inputPlaces)
                {
                    AddDelta(deltasByPlace, placeId, -(int)InMatrix[placeId, transitionId]);
                }
            }

            foreach (var deltaByPlace in deltasByPlace)
            {
                result[deltaByPlace.Key] = m[deltaByPlace.Key] + deltaByPlace.Value;
            }
        }
        catch
        {
            deltaLease.MarkFaulted();
            throw;
        }

        DispatchFiringPlan(firingPlan, TransitionFunctions);

        return result;
    }
    #endregion

    [ContractInvariantMethod]
    protected void ObjectInvariant()
    {
        Contract.Invariant(Places != null, "Places can never be null");
        Contract.Invariant(Transitions != null, "Transitions must never be null");
        Contract.Invariant(!string.IsNullOrEmpty(Id), "the petri net must have a valid ID");
        Contract.Invariant(InMatrix != null, "the in matrix must be a valid matrix");
        Contract.Invariant(InMatrix!.ColumnCount == Transitions!.Count, "in matrix is the wrong width");
        Contract.Invariant(InMatrix.RowCount == Places!.Count, "in matrix is the wrong height");
        Contract.Invariant(OutMatrix != null, "the out matrix must be a valid matrix");
        Contract.Invariant(OutMatrix!.ColumnCount == Transitions.Count, "out matrix is the wrong width");
        Contract.Invariant(OutMatrix.RowCount == Places.Count, "out matrix is the wrong height");
    }

    public int GetTransitionPriority(int t)
    {
        return TransitionPriorities.ContainsKey(t) ? TransitionPriorities[t] : 0;
    }
    public override IEnumerable<int> GetPlaceOutArcs(int placeId)
    {
        if (!placeOutTransitions.TryGetValue(placeId, out var transitionIds))
        {
            yield break;
        }

        foreach (var transitionId in transitionIds)
        {
            yield return transitionId;
        }
    }

    public override IEnumerable<int> AllPlaces()
    {
        return Places.Count() > 0 ? Places.Keys.AsEnumerable() : new int[] { };
    }

    public IEnumerable<ConflictSet> GetConflictingTransitions()
    {
        throw new NotImplementedException();
    }

    void BuildSparseConnectivityIndexes(
        IReadOnlyDictionary<int, List<InArc>> inArcs,
        IReadOnlyDictionary<int, List<OutArc>> outArcs)
    {
        nonInhibitorPlacesByTransition.Clear();
        inhibitorPlacesByTransition.Clear();
        outputPlacesByTransition.Clear();
        effectiveSupportByTransition.Clear();
        placeOutTransitions.Clear();

        foreach (var transitionId in Transitions.Keys)
        {
            var inputs = new HashSet<int>();
            var inhibitors = new HashSet<int>();
            var outputs = new HashSet<int>();

            if (inArcs.TryGetValue(transitionId, out var transitionInArcs))
            {
                foreach (var inArc in transitionInArcs)
                {
                    if (inArc.IsInhibitor)
                    {
                        inhibitors.Add(inArc.Source);
                    }
                    else
                    {
                        inputs.Add(inArc.Source);
                        AddToIndex(placeOutTransitions, inArc.Source, transitionId);
                    }
                }
            }

            if (outArcs.TryGetValue(transitionId, out var transitionOutArcs))
            {
                foreach (var outArc in transitionOutArcs)
                {
                    outputs.Add(outArc.Target);
                }
            }

            nonInhibitorPlacesByTransition[transitionId] = inputs;
            inhibitorPlacesByTransition[transitionId] = inhibitors;
            outputPlacesByTransition[transitionId] = outputs;
            effectiveSupportByTransition[transitionId] = new HashSet<int>(inputs.Concat(outputs));
        }
    }

    static void AddToIndex(Dictionary<int, HashSet<int>> index, int key, int value)
    {
        if (!index.TryGetValue(key, out var values))
        {
            values = new HashSet<int>();
            index[key] = values;
        }

        values.Add(value);
    }

    static void AddDelta(Dictionary<int, int> deltasByPlace, int placeId, int delta)
    {
        if (!deltasByPlace.TryGetValue(placeId, out var existingDelta))
        {
            deltasByPlace[placeId] = delta;
            return;
        }

        deltasByPlace[placeId] = existingDelta + delta;
    }
}

