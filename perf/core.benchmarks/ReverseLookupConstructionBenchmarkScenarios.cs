namespace core.benchmarks;

using petrinets2.core;

public sealed record ReverseLookupConstructionScenario(string[] PlaceNames, string[] TransitionNames, int ArcCount);

public static class ReverseLookupConstructionBenchmarkScenarios
{
    public static ReverseLookupConstructionScenario Create(int placeCount, int transitionCount, int fanOut)
    {
        if (placeCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placeCount));
        }

        if (transitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionCount));
        }

        if (fanOut <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fanOut));
        }

        var placeNames = Enumerable.Range(0, placeCount).Select(index => $"p{index}").ToArray();
        var transitionNames = Enumerable.Range(0, transitionCount).Select(index => $"t{index}").ToArray();
        var arcCount = transitionCount * fanOut;

        return new ReverseLookupConstructionScenario(placeNames, transitionNames, arcCount);
    }

    public static GraphPetriNet BuildGraphNet(ReverseLookupConstructionScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var builder = CreatePetriNet.Called("reverse-lookup-construction")
            .WithPlaces(scenario.PlaceNames)
            .WithTransitions(scenario.TransitionNames);

        for (var transitionIndex = 0; transitionIndex < scenario.TransitionNames.Length; transitionIndex++)
        {
            var transition = scenario.TransitionNames[transitionIndex];
            for (var edge = 0; edge < scenario.ArcCount / scenario.TransitionNames.Length; edge++)
            {
                var source = scenario.PlaceNames[(transitionIndex + edge) % scenario.PlaceNames.Length];
                var target = scenario.PlaceNames[(transitionIndex + edge + 1) % scenario.PlaceNames.Length];
                builder.AddInArc(source, transition, false, 1);
                builder.AddOutArc(transition, target, 1);
            }
        }

        return builder.CreateNet<GraphPetriNet>();
    }

    public static MatrixPetriNet BuildMatrixNet(ReverseLookupConstructionScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var builder = CreatePetriNet.Called("reverse-lookup-construction")
            .WithPlaces(scenario.PlaceNames)
            .WithTransitions(scenario.TransitionNames);

        for (var transitionIndex = 0; transitionIndex < scenario.TransitionNames.Length; transitionIndex++)
        {
            var transition = scenario.TransitionNames[transitionIndex];
            for (var edge = 0; edge < scenario.ArcCount / scenario.TransitionNames.Length; edge++)
            {
                var source = scenario.PlaceNames[(transitionIndex + edge) % scenario.PlaceNames.Length];
                var target = scenario.PlaceNames[(transitionIndex + edge + 1) % scenario.PlaceNames.Length];
                builder.AddInArc(source, transition, false, 1);
                builder.AddOutArc(transition, target, 1);
            }
        }

        return builder.CreateNet<MatrixPetriNet>();
    }
}
