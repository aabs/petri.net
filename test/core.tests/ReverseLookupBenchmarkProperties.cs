namespace core.tests;

using core.benchmarks;

public class ReverseLookupBenchmarkProperties
{
    [Property]
    public bool ConstructionScenarioBuilder_IsDeterministic(PositiveInt placeSeed, PositiveInt transitionSeed)
    {
        var placeCount = (placeSeed.Get % 64) + 8;
        var transitionCount = (transitionSeed.Get % 64) + 8;

        var first = ReverseLookupConstructionBenchmarkScenarios.Create(placeCount, transitionCount, 2);
        var second = ReverseLookupConstructionBenchmarkScenarios.Create(placeCount, transitionCount, 2);

        return first.PlaceNames.SequenceEqual(second.PlaceNames)
            && first.TransitionNames.SequenceEqual(second.TransitionNames)
            && first.ArcCount == second.ArcCount;
    }

    [Property]
    public bool PnmlScenarioBuilder_IsDeterministic(PositiveInt placeSeed, PositiveInt transitionSeed)
    {
        var placeCount = (placeSeed.Get % 64) + 8;
        var transitionCount = (transitionSeed.Get % 64) + 8;

        var first = PnmlLoadBenchmarkScenarios.Create(placeCount, transitionCount);
        var second = PnmlLoadBenchmarkScenarios.Create(placeCount, transitionCount);

        return first.ExpectedMarkings.OrderBy(pair => pair.Key).SequenceEqual(second.ExpectedMarkings.OrderBy(pair => pair.Key))
            && first.PlaceIdsByName.OrderBy(pair => pair.Key).SequenceEqual(second.PlaceIdsByName.OrderBy(pair => pair.Key))
            && first.TransitionIdsByName.OrderBy(pair => pair.Key).SequenceEqual(second.TransitionIdsByName.OrderBy(pair => pair.Key));
    }

    [Property]
    public bool PnmlScenarioFixture_LoadsDeterministically(PositiveInt placeSeed, PositiveInt transitionSeed)
    {
        var placeCount = (placeSeed.Get % 32) + 8;
        var transitionCount = (transitionSeed.Get % 32) + 8;

        var scenario = PnmlLoadBenchmarkScenarios.Create(placeCount, transitionCount);
        var first = PnmlModelLoader.Load(scenario.PnmlPath).Single();
        var second = PnmlModelLoader.Load(scenario.PnmlPath).Single();

        return first.Places.OrderBy(pair => pair.Key).SequenceEqual(second.Places.OrderBy(pair => pair.Key))
            && first.Transitions.OrderBy(pair => pair.Key).SequenceEqual(second.Transitions.OrderBy(pair => pair.Key))
            && first.InArcs.Count == second.InArcs.Count
            && first.OutArcs.Count == second.OutArcs.Count;
    }
}
