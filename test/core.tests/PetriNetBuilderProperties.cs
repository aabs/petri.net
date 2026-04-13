namespace core.tests;

public class PetriNetBuilderProperties
{
    [Property]
    public bool FluentMethods_ReturnSameBuilderInstance(PositiveInt placeSeed, PositiveInt transitionSeed)
    {
        var placeCount = (placeSeed.Get % 12) + 1;
        var transitionCount = (transitionSeed.Get % 12) + 1;
        var scenario = PnmlStreamingPropertyData.CreateBuilderCase(placeCount, transitionCount);

        var builder = new PetriNetBuilder();
        var fluent = builder
            .WithName(scenario.NetName)
            .WithPlaces(scenario.PlaceNames)
            .WithTransitions(scenario.TransitionNames);

        return ReferenceEquals(builder, fluent)
            && ReferenceEquals(builder, builder.AddingPlace("p-extra"))
            && ReferenceEquals(builder, builder.AddingTransition("t-extra"));
    }

    [Property]
    public bool BuildGraphAndBuildMatrix_StayStructurallyEquivalent(PositiveInt placeSeed, PositiveInt transitionSeed)
    {
        var placeCount = (placeSeed.Get % 24) + 2;
        var transitionCount = (transitionSeed.Get % 24) + 1;
        var scenario = PnmlStreamingPropertyData.CreateBuilderCase(placeCount, transitionCount);

        var builder = BuildScenario(scenario);
        var graph = builder.BuildGraph();
        var matrix = builder.BuildMatrix();

        return graph.Places.OrderBy(pair => pair.Key).SequenceEqual(matrix.Places.OrderBy(pair => pair.Key))
            && graph.Transitions.OrderBy(pair => pair.Key).SequenceEqual(matrix.Transitions.OrderBy(pair => pair.Key))
            && graph.InArcs.Count == matrix.Transitions.Count
            && graph.OutArcs.Count == matrix.Transitions.Count;
    }

    [Property]
    public bool BuildOutputs_KeepArcEndpointsInsidePlaceIndexDomain(PositiveInt placeSeed, PositiveInt transitionSeed)
    {
        var placeCount = (placeSeed.Get % 20) + 2;
        var transitionCount = (transitionSeed.Get % 20) + 1;
        var scenario = PnmlStreamingPropertyData.CreateBuilderCase(placeCount, transitionCount);

        var builder = BuildScenario(scenario);
        var graph = builder.BuildGraph();

        var inArcsValid = graph.InArcs.Values.SelectMany(arcs => arcs).All(arc => graph.Places.ContainsKey(arc.Source));
        var outArcsValid = graph.OutArcs.Values.SelectMany(arcs => arcs).All(arc => graph.Places.ContainsKey(arc.Target));
        var marking = builder.BuildInitialMarking();

        return inArcsValid
            && outArcsValid
            && marking.Size == graph.Places.Count;
    }

    static PetriNetBuilder BuildScenario(PetriNetBuilderCase scenario)
    {
        var builder = new PetriNetBuilder()
            .WithName(scenario.NetName)
            .WithPlaces(scenario.PlaceNames)
            .WithTransitions(scenario.TransitionNames);

        foreach (var place in scenario.PlaceNames)
        {
            builder.WithPlace(place)
                .WithInitialMarking(scenario.InitialMarkings[place])
                .WithCapacity(scenario.Capacities[place]);
        }

        foreach (var inArc in scenario.InputArcs)
        {
            builder.AddingInputArc(inArc.SourcePlace, inArc.TargetTransition, inArc.Weight, inArc.IsInhibitor);
        }

        foreach (var outArc in scenario.OutputArcs)
        {
            builder.AddingOutputArc(outArc.SourceTransition, outArc.TargetPlace, outArc.Weight);
        }

        return builder;
    }
}
