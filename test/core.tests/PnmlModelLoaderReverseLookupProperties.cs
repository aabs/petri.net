namespace core.tests;

using core.benchmarks;

#pragma warning disable CS0618

public class PnmlModelLoaderReverseLookupProperties
{
    [Property]
    public bool Load_ResolvesArcPlaceIdsFromLookupMap(PositiveInt placeSeed, PositiveInt transitionSeed)
    {
        var placeCount = (placeSeed.Get % 32) + 8;
        var transitionCount = (transitionSeed.Get % 32) + 8;
        var scenario = PnmlLoadBenchmarkScenarios.Create(placeCount, transitionCount);

        var net = PnmlModelLoader.Load(scenario.PnmlPath).Single();

        var placeIds = net.Places.Values.ToHashSet(StringComparer.Ordinal);
        var everyInArcResolvesToKnownPlace = net.InArcs.Values.SelectMany(values => values).All(arc => net.Places.ContainsKey(arc.Source));
        var everyOutArcResolvesToKnownPlace = net.OutArcs.Values.SelectMany(values => values).All(arc => net.Places.ContainsKey(arc.Target));

        return scenario.ExpectedMarkings.Keys.All(placeIds.Contains)
            && everyInArcResolvesToKnownPlace
            && everyOutArcResolvesToKnownPlace;
    }

    [Property]
    public bool LoadMarkings_ResolvesPlaceIdsAndThrowsOnMissingPlace(PositiveInt placeSeed, PositiveInt transitionSeed)
    {
        var placeCount = (placeSeed.Get % 32) + 8;
        var transitionCount = (transitionSeed.Get % 32) + 8;
        var scenario = PnmlLoadBenchmarkScenarios.Create(placeCount, transitionCount);

        var nets = PnmlModelLoader.Load(scenario.PnmlPath).ToList();
        var markings = PnmlModelLoader.LoadMarkings(scenario.PnmlPath, nets);
        var marking = markings.Values.Single();
        var net = nets.Single();

        var parity = scenario.ExpectedMarkings.All(pair =>
        {
            var index = net.Places.Single(place => place.Value == pair.Key).Key;
            return marking[index] == pair.Value;
        });

        var failurePath = PnmlLoadBenchmarkScenarios.CreateMarkingFailureCase(net.Id);
        var failure = Record.Exception(() => PnmlModelLoader.LoadMarkings(failurePath, nets));

        return parity
            && failure is KeyNotFoundException keyNotFoundException
            && keyNotFoundException.Message.Contains("missing-place-id", StringComparison.Ordinal);
    }
}

#pragma warning restore CS0618
