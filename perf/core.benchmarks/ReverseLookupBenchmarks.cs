namespace core.benchmarks;

using BenchmarkDotNet.Attributes;
using petrinets2.core;

[MemoryDiagnoser]
public class ReverseLookupBenchmarks
{
    ReverseLookupConstructionScenario constructionScenario = null!;
    PnmlLoadScenario pnmlScenario = null!;
    List<GraphPetriNet> loadedNets = null!;

    [Params(128, 512, 1000)]
    public int PlaceCount { get; set; }

    [Params(128, 512, 1000)]
    public int TransitionCount { get; set; }

    [Params(2, 4)]
    public int FanOut { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        constructionScenario = ReverseLookupConstructionBenchmarkScenarios.Create(PlaceCount, TransitionCount, FanOut);
        pnmlScenario = PnmlLoadBenchmarkScenarios.Create(Math.Max(PlaceCount, 16), Math.Max(TransitionCount, 16));
        loadedNets = PnmlModelLoader.Load(pnmlScenario.PnmlPath).ToList();
    }

    [Benchmark]
    public GraphPetriNet Construction_GraphNet()
    {
        return ReverseLookupConstructionBenchmarkScenarios.BuildGraphNet(constructionScenario);
    }

    [Benchmark]
    public MatrixPetriNet Construction_MatrixNet()
    {
        return ReverseLookupConstructionBenchmarkScenarios.BuildMatrixNet(constructionScenario);
    }

    [Benchmark]
    public List<GraphPetriNet> Pnml_Load()
    {
        return PnmlModelLoader.Load(pnmlScenario.PnmlPath).ToList();
    }

    [Benchmark]
    public IDictionary<string, Marking> Pnml_LoadMarkings()
    {
        return PnmlModelLoader.LoadMarkings(pnmlScenario.PnmlPath, loadedNets);
    }
}
