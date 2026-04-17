namespace core.benchmarks;

using petrinets2.core;

public sealed record PetriNetBuilderBenchmarkScenario(
    string[] PlaceNames,
    string[] TransitionNames,
    (string SourcePlace, string TargetTransition, int Weight, bool IsInhibitor)[] InArcs,
    (string SourceTransition, string TargetPlace, int Weight)[] OutArcs);

public static class PetriNetBuilderBenchmarkScenarios
{
    public static PetriNetBuilderBenchmarkScenario Create(int placeCount, int transitionCount)
    {
        if (placeCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placeCount));
        }

        if (transitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionCount));
        }

        var places = Enumerable.Range(0, placeCount).Select(index => $"p{index}").ToArray();
        var transitions = Enumerable.Range(0, transitionCount).Select(index => $"t{index}").ToArray();
        var inArcs = new (string SourcePlace, string TargetTransition, int Weight, bool IsInhibitor)[transitionCount];
        var outArcs = new (string SourceTransition, string TargetPlace, int Weight)[transitionCount];

        for (var i = 0; i < transitionCount; i++)
        {
            var weight = (i % 4) + 1;
            inArcs[i] = (places[i % places.Length], transitions[i], weight, i % 11 == 0);
            outArcs[i] = (transitions[i], places[(i + 1) % places.Length], weight);
        }

        return new PetriNetBuilderBenchmarkScenario(places, transitions, inArcs, outArcs);
    }

    public static PetriNetBuilder BuildBuilder(PetriNetBuilderBenchmarkScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var builder = new PetriNetBuilder().WithName("benchmark")
            .WithPlaces(scenario.PlaceNames)
            .WithTransitions(scenario.TransitionNames);

        foreach (var arc in scenario.InArcs)
        {
            builder.AddingInputArc(arc.SourcePlace, arc.TargetTransition, arc.Weight, arc.IsInhibitor);
        }

        foreach (var arc in scenario.OutArcs)
        {
            builder.AddingOutputArc(arc.SourceTransition, arc.TargetPlace, arc.Weight);
        }

        return builder;
    }
}
