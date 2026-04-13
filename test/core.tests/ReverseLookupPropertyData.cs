namespace core.tests;

public static class ReverseLookupPropertyData
{
    public static string[] BuildNameBatch(string prefix, int uniqueCount, int duplicateStride)
    {
        if (uniqueCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(uniqueCount));
        }

        if (duplicateStride <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(duplicateStride));
        }

        var values = new List<string>(uniqueCount + (uniqueCount / duplicateStride));
        for (var i = 0; i < uniqueCount; i++)
        {
            var name = $"{prefix}{i}";
            values.Add(name);
            if ((i + 1) % duplicateStride == 0)
            {
                values.Add(name);
            }
        }

        return values.ToArray();
    }

    public static CreatePetriNet CreateConnectedBuilder(int placeCount, int transitionCount)
    {
        if (placeCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placeCount));
        }

        if (transitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionCount));
        }

        var builder = CreatePetriNet.Called("reverse-lookup");
        var placeNames = Enumerable.Range(0, placeCount).Select(index => $"p{index}").ToArray();
        var transitionNames = Enumerable.Range(0, transitionCount).Select(index => $"t{index}").ToArray();

        builder.WithPlaces(placeNames);
        builder.WithTransitions(transitionNames);

        for (var transitionIndex = 0; transitionIndex < transitionCount; transitionIndex++)
        {
            var source = placeNames[transitionIndex % placeNames.Length];
            var target = placeNames[(transitionIndex + 1) % placeNames.Length];
            var transition = transitionNames[transitionIndex];

            builder.AddInArc(source, transition, false, 1);
            builder.AddOutArc(transition, target, 1);
        }

        return builder;
    }
}
