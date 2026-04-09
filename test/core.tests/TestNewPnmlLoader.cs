using System.Collections;
using System.Reflection;

namespace core.tests;

public class TestNewPnmlLoader
{
    [Fact]
    public void TestLoad1()
    {
        var loader = new NewPnmlLoader<GraphPetriNet>();
        var path = PnmlLoaderDataGenerator.ExtractEmbeddedPnml("p0).xml");
        var model = loader.Load(path);

        Assert.Single(model);
    }

    [Theory]
    [MemberData(nameof(PnmlLoaderDataGenerator.FullSpecs), MemberType = typeof(PnmlLoaderDataGenerator))]
    public void TestLoadPnmlFile(string path, IEnumerable<string> markingProgression)
    {
        var loader = new NewPnmlLoader<GraphPetriNet>();
        var netlist = loader.Load(path).ToList();

        Assert.Single(netlist);
        var net = netlist[0];
        Assert.NotNull(net);

        foreach (var _ in markingProgression)
        {
            // Placeholder: historical marking progression assertions were never implemented.
        }
    }
}

public static class PnmlLoaderDataGenerator
{
    private static readonly Assembly Assembly = typeof(TestNewPnmlLoader).Assembly;

    public static IEnumerable<object[]> FullSpecs()
    {
        var manifestNames = Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("pnml/", StringComparison.Ordinal) && name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        foreach (var manifestName in manifestNames)
        {
            var fileName = Path.GetFileName(manifestName);
            yield return new object[] { ExtractEmbeddedPnml(fileName), Array.Empty<string>() };
        }
    }

    public static string ExtractEmbeddedPnml(string fileName)
    {
        var resourceName = $"pnml/{fileName}";
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded PNML resource not found: {resourceName}");

        var targetPath = Path.Combine(Path.GetTempPath(), "petrinets2-pnml-tests", Guid.NewGuid().ToString("N") + "-" + fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        using var file = File.Create(targetPath);
        stream.CopyTo(file);
        return targetPath;
    }
}
