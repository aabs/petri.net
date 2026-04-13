namespace core.tests;

using System.Collections.Concurrent;

public class CreatePetriNetReverseLookupProperties
{
    [Property]
    public bool PlaceIndex_MatchesForwardMap(string[] placeNameBatches)
    {
        if (placeNameBatches.Length == 0 || placeNameBatches.Any(string.IsNullOrWhiteSpace))
        {
            return true;
        }

        var normalized = placeNameBatches.Select(Normalize).Where(value => value.Length > 0).ToArray();
        if (normalized.Length == 0)
        {
            return true;
        }

        var builder = CreatePetriNet.Called("reverse-lookup").WithPlaces(normalized);
        return builder.Places.All(pair => builder.PlaceIndex(pair.Value) == pair.Key);
    }

    [Property]
    public bool TransitionIndex_MatchesForwardMap(string[] transitionNameBatches)
    {
        if (transitionNameBatches.Length == 0 || transitionNameBatches.Any(string.IsNullOrWhiteSpace))
        {
            return true;
        }

        var normalized = transitionNameBatches.Select(Normalize).Where(value => value.Length > 0).ToArray();
        if (normalized.Length == 0)
        {
            return true;
        }

        var builder = CreatePetriNet.Called("reverse-lookup").WithTransitions(normalized);
        return builder.Transitions.All(pair => builder.TransitionIndex(pair.Value) == pair.Key);
    }

    [Property]
    public bool MissingNameLookups_ThrowActionableKeyNotFound()
    {
        var builder = CreatePetriNet.Called("reverse-lookup")
            .WithPlaces("p0")
            .WithTransitions("t0");

        var placeFailure = Record.Exception(() => builder.PlaceIndex("missing-place"));
        var transitionFailure = Record.Exception(() => builder.TransitionIndex("missing-transition"));

        return placeFailure is KeyNotFoundException placeException
            && transitionFailure is KeyNotFoundException transitionException
            && placeException.Message.Contains("missing-place", StringComparison.Ordinal)
            && transitionException.Message.Contains("missing-transition", StringComparison.Ordinal);
    }

    [Property]
    public bool ConcurrentLookupAndMutation_RemainsConsistent(PositiveInt placeSeed, PositiveInt transitionSeed)
    {
        var placeCount = (placeSeed.Get % 24) + 8;
        var transitionCount = (transitionSeed.Get % 24) + 8;
        var builder = ReverseLookupPropertyData.CreateConnectedBuilder(placeCount, transitionCount);

        var places = builder.Places.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
        var transitions = builder.Transitions.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
        var failures = new ConcurrentBag<Exception>();

        Parallel.For(0, 64, iteration =>
        {
            try
            {
                var place = places[iteration % places.Length];
                var transition = transitions[iteration % transitions.Length];

                _ = builder.PlaceIndex(place);
                _ = builder.TransitionIndex(transition);
                builder.AddInArc(place, transition, false, 1);
                builder.AddOutArc(transition, place, 1);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        });

        return failures.IsEmpty
            && builder.Places.All(pair => builder.PlaceIndex(pair.Value) == pair.Key)
            && builder.Transitions.All(pair => builder.TransitionIndex(pair.Value) == pair.Key)
            && builder.InArcs.Values.SelectMany(arcs => arcs).All(arc => builder.Places.ContainsKey(arc.Source))
            && builder.OutArcs.Values.SelectMany(arcs => arcs).All(arc => builder.Places.ContainsKey(arc.Target));
    }

    [Property]
    public bool WithPlaces_IgnoresDuplicatesAndKeepsStableIndices(PositiveInt sizeSeed)
    {
        var uniqueCount = (sizeSeed.Get % 32) + 4;
        var firstBatch = ReverseLookupPropertyData.BuildNameBatch("p", uniqueCount, 3);
        var secondBatch = ReverseLookupPropertyData.BuildNameBatch("p", uniqueCount, 2);

        var builder = CreatePetriNet.Called("reverse-lookup")
            .WithPlaces(firstBatch);

        var baseline = builder.Places.ToDictionary(pair => pair.Key, pair => pair.Value);

        builder.WithPlaces(secondBatch);

        var uniqueNames = firstBatch.Concat(secondBatch).Distinct(StringComparer.Ordinal).ToArray();

        return builder.Places.Count == uniqueNames.Length
            && baseline.All(pair => builder.PlaceIndex(pair.Value) == pair.Key)
            && builder.Places.All(pair => builder.PlaceIndex(pair.Value) == pair.Key);
    }

    [Property]
    public bool WithTransitions_IgnoresDuplicatesAndKeepsStableIndices(PositiveInt sizeSeed)
    {
        var uniqueCount = (sizeSeed.Get % 32) + 4;
        var firstBatch = ReverseLookupPropertyData.BuildNameBatch("t", uniqueCount, 3);
        var secondBatch = ReverseLookupPropertyData.BuildNameBatch("t", uniqueCount, 2);

        var builder = CreatePetriNet.Called("reverse-lookup")
            .WithTransitions(firstBatch);

        var baseline = builder.Transitions.ToDictionary(pair => pair.Key, pair => pair.Value);

        builder.WithTransitions(secondBatch);

        var uniqueNames = firstBatch.Concat(secondBatch).Distinct(StringComparer.Ordinal).ToArray();

        return builder.Transitions.Count == uniqueNames.Length
            && baseline.All(pair => builder.TransitionIndex(pair.Value) == pair.Key)
            && builder.Transitions.All(pair => builder.TransitionIndex(pair.Value) == pair.Key);
    }

    [Property]
    public bool ForwardReverseBijection_HoldsAfterMixedOperations(PositiveInt placeSeed, PositiveInt transitionSeed)
    {
        var placeCount = (placeSeed.Get % 32) + 4;
        var transitionCount = (transitionSeed.Get % 32) + 4;
        var builder = ReverseLookupPropertyData.CreateConnectedBuilder(placeCount, transitionCount);

        var duplicatePlaces = ReverseLookupPropertyData.BuildNameBatch("p", placeCount, 2);
        var duplicateTransitions = ReverseLookupPropertyData.BuildNameBatch("t", transitionCount, 2);

        builder.WithPlaces(duplicatePlaces);
        builder.WithTransitions(duplicateTransitions);

        return builder.Places.Count == builder.Places.Values.Distinct(StringComparer.Ordinal).Count()
            && builder.Transitions.Count == builder.Transitions.Values.Distinct(StringComparer.Ordinal).Count()
            && builder.Places.All(pair => builder.PlaceIndex(pair.Value) == pair.Key)
            && builder.Transitions.All(pair => builder.TransitionIndex(pair.Value) == pair.Key);
    }

    static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var buffer = new char[input.Length];
        var cursor = 0;
        foreach (var ch in input)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[cursor++] = ch;
            }
        }

        return cursor == 0 ? string.Empty : new string(buffer, 0, cursor);
    }
}
