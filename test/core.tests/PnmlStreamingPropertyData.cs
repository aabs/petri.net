namespace core.tests;

public sealed record PetriNetBuilderCase(
    string NetName,
    string[] PlaceNames,
    string[] TransitionNames,
    (string SourcePlace, string TargetTransition, int Weight, bool IsInhibitor)[] InputArcs,
    (string SourceTransition, string TargetPlace, int Weight)[] OutputArcs,
    IReadOnlyDictionary<string, int> InitialMarkings,
    IReadOnlyDictionary<string, int> Capacities);

public sealed record PnmlStreamingCase(string Path, string[] NetIds);

public static class PnmlStreamingPropertyData
{
    public static PetriNetBuilderCase CreateBuilderCase(int placeCount, int transitionCount)
    {
        if (placeCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placeCount));
        }

        if (transitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionCount));
        }

        var placeNames = Enumerable.Range(0, placeCount).Select(index => $"p{index}").ToArray();
        var transitionNames = Enumerable.Range(0, transitionCount).Select(index => $"t{index}").ToArray();

        var inArcs = new (string SourcePlace, string TargetTransition, int Weight, bool IsInhibitor)[transitionCount];
        var outArcs = new (string SourceTransition, string TargetPlace, int Weight)[transitionCount];
        var markings = new Dictionary<string, int>(placeCount, StringComparer.Ordinal);
        var capacities = new Dictionary<string, int>(placeCount, StringComparer.Ordinal);

        for (var i = 0; i < transitionCount; i++)
        {
            var sourcePlace = placeNames[i % placeNames.Length];
            var targetPlace = placeNames[(i + 1) % placeNames.Length];
            var transition = transitionNames[i];
            var weight = (i % 3) + 1;

            inArcs[i] = (sourcePlace, transition, weight, i % 7 == 0);
            outArcs[i] = (transition, targetPlace, weight);
        }

        for (var i = 0; i < placeNames.Length; i++)
        {
            markings[placeNames[i]] = i % 4;
            capacities[placeNames[i]] = i % 6;
        }

        return new PetriNetBuilderCase(
            "builder-case",
            placeNames,
            transitionNames,
            inArcs,
            outArcs,
            markings,
            capacities);
    }

    public static PnmlStreamingCase CreateStreamingCase(int netCount, int placeCount, int transitionCount)
    {
        if (netCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(netCount));
        }

        if (placeCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(placeCount));
        }

        if (transitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionCount));
        }

        var netIds = new string[netCount];
        var lines = new List<string>
        {
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
            "<pnml>"
        };

        for (var netIndex = 0; netIndex < netCount; netIndex++)
        {
            var netId = $"net-{netIndex}";
            netIds[netIndex] = netId;
            lines.Add($"  <net id=\"{netId}\" type=\"P/T net\">");

            for (var placeIndex = 0; placeIndex < placeCount; placeIndex++)
            {
                lines.Add($"    <place id=\"P{netIndex}_{placeIndex}\">");
                lines.Add($"      <initialMarking><value>{placeIndex % 3}</value></initialMarking>");
                lines.Add("    </place>");
            }

            for (var transitionIndex = 0; transitionIndex < transitionCount; transitionIndex++)
            {
                lines.Add($"    <transition id=\"T{netIndex}_{transitionIndex}\" />");
            }

            for (var transitionIndex = 0; transitionIndex < transitionCount; transitionIndex++)
            {
                lines.Add($"    <arc id=\"A_IN_{netIndex}_{transitionIndex}\" source=\"P{netIndex}_{transitionIndex % placeCount}\" target=\"T{netIndex}_{transitionIndex}\">");
                lines.Add($"      <inscription><value>{(transitionIndex % 2) + 1}</value></inscription>");
                lines.Add("    </arc>");
                lines.Add($"    <arc id=\"A_OUT_{netIndex}_{transitionIndex}\" source=\"T{netIndex}_{transitionIndex}\" target=\"P{netIndex}_{(transitionIndex + 1) % placeCount}\" />");
            }

            lines.Add("  </net>");
        }

        lines.Add("</pnml>");

        var path = Path.Combine(Path.GetTempPath(), "petrinets2-streaming-tests", $"streaming-{Guid.NewGuid():N}.pnml");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, lines);

        return new PnmlStreamingCase(path, netIds);
    }

    public static string CreateDuplicatePlaceIdCase()
    {
        return WritePnml(
            "<pnml>\n"
            + "  <net id=\"duplicate-place\" type=\"P/T net\">\n"
            + "    <place id=\"P0\" />\n"
            + "    <place id=\"P0\" />\n"
            + "    <transition id=\"T0\" />\n"
            + "  </net>\n"
            + "</pnml>");
    }

    public static string CreateMissingArcEndpointCase()
    {
        return WritePnml(
            "<pnml>\n"
            + "  <net id=\"missing-endpoint\" type=\"P/T net\">\n"
            + "    <place id=\"P0\" />\n"
            + "    <transition id=\"T0\" />\n"
            + "    <arc id=\"A0\" source=\"P0\" target=\"T_missing\" />\n"
            + "  </net>\n"
            + "</pnml>");
    }

    public static IEnumerable<string> EmbeddedCorpusPaths()
    {
        foreach (var row in PnmlLoaderDataGenerator.FullSpecs())
        {
            yield return (string)row[0];
        }
    }

    static string WritePnml(string body)
    {
        var path = Path.Combine(Path.GetTempPath(), "petrinets2-streaming-tests", $"invalid-{Guid.NewGuid():N}.pnml");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, body);
        return path;
    }
}
