namespace core.benchmarks;

using System.Text;
using petrinets2.core;

public sealed record PnmlLoadScenario(
    string PnmlPath,
    IReadOnlyDictionary<string, int> ExpectedMarkings,
    IReadOnlyDictionary<string, string> PlaceIdsByName,
    IReadOnlyDictionary<string, string> TransitionIdsByName);

public static class PnmlLoadBenchmarkScenarios
{
    public static PnmlLoadScenario Create(int placeCount, int transitionCount)
    {
        if (placeCount <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(placeCount));
        }

        if (transitionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionCount));
        }

        var netId = $"net-{placeCount}-{transitionCount}";
        var placeIdsByName = new Dictionary<string, string>(placeCount, StringComparer.Ordinal);
        var transitionIdsByName = new Dictionary<string, string>(transitionCount, StringComparer.Ordinal);
        var expectedMarkings = new Dictionary<string, int>(placeCount, StringComparer.Ordinal);

        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml.AppendLine("<pnml xmlns=\"http://www.example.org/pnml\">");
        xml.AppendLine($"  <net id=\"{netId}\" type=\"http://www.example.org/pnml/PTNet\">");

        for (var placeIndex = 0; placeIndex < placeCount; placeIndex++)
        {
            var placeName = $"p{placeIndex}";
            var placeId = $"P{placeIndex}";
            var marking = placeIndex % 3;

            placeIdsByName[placeName] = placeId;
            expectedMarkings[placeId] = marking;

            xml.AppendLine($"    <place id=\"{placeId}\">");
            xml.AppendLine("      <name><text>" + placeName + "</text></name>");
            xml.AppendLine("      <initialMarking><text>" + marking + "</text></initialMarking>");
            xml.AppendLine("    </place>");
        }

        for (var transitionIndex = 0; transitionIndex < transitionCount; transitionIndex++)
        {
            var transitionName = $"t{transitionIndex}";
            var transitionId = $"T{transitionIndex}";
            transitionIdsByName[transitionName] = transitionId;
            xml.AppendLine($"    <transition id=\"{transitionId}\"><name><text>{transitionName}</text></name></transition>");
        }

        for (var transitionIndex = 0; transitionIndex < transitionCount; transitionIndex++)
        {
            var transitionId = transitionIdsByName[$"t{transitionIndex}"];
            var sourcePlaceId = $"P{transitionIndex % placeCount}";
            var targetPlaceId = $"P{(transitionIndex + 1) % placeCount}";

            xml.AppendLine($"    <arc id=\"A_in_{transitionIndex}\" source=\"{sourcePlaceId}\" target=\"{transitionId}\" />");
            xml.AppendLine($"    <arc id=\"A_out_{transitionIndex}\" source=\"{transitionId}\" target=\"{targetPlaceId}\" />");
        }

        xml.AppendLine("  </net>");
        xml.AppendLine("</pnml>");

        var filePath = Path.Combine(Path.GetTempPath(), "petrinets2-reverse-lookup", $"{netId}-{Guid.NewGuid():N}.pnml");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, xml.ToString());

        return new PnmlLoadScenario(filePath, expectedMarkings, placeIdsByName, transitionIdsByName);
    }

    public static string CreateMarkingFailureCase(string netId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(netId);

        var path = Path.Combine(Path.GetTempPath(), "petrinets2-reverse-lookup", $"missing-place-id-{netId}-{Guid.NewGuid():N}.pnml");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        const string content = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
            + "<pnml xmlns=\"http://www.example.org/pnml\">\n"
            + "  <net id=\"__NET_ID__\" type=\"http://www.example.org/pnml/PTNet\">\n"
            + "    <place id=\"missing-place-id\"><name><text>p0</text></name><initialMarking><text>1</text></initialMarking></place>\n"
            + "    <transition id=\"t0\"><name><text>t0</text></name></transition>\n"
            + "  </net>\n"
            + "</pnml>\n";

        File.WriteAllText(path, content.Replace("__NET_ID__", netId, StringComparison.Ordinal));
        return path;
    }
}
