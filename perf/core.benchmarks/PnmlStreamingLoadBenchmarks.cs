namespace core.benchmarks;

using BenchmarkDotNet.Attributes;
using petrinets2.core;

#pragma warning disable CS0618

[MemoryDiagnoser]
public class PnmlStreamingLoadBenchmarks
{
    PnmlLoadScenario scenario = null!;
    PnmlStreamingModelLoader streamingLoader = null!;

    [Params(256, 1024)]
    public int PlaceCount { get; set; }

    [Params(256, 1024)]
    public int TransitionCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        scenario = PnmlLoadBenchmarkScenarios.Create(PlaceCount, TransitionCount);
        streamingLoader = new PnmlStreamingModelLoader();
    }

    [Benchmark]
    public List<GraphPetriNet> Legacy_PnmlModelLoader_Load()
    {
        return PnmlModelLoader.Load(scenario.PnmlPath).ToList();
    }

    [Benchmark]
    public IReadOnlyList<GraphPetriNet> Streaming_LoadGraph()
    {
        return streamingLoader.LoadGraph(scenario.PnmlPath);
    }

    [Benchmark]
    public IReadOnlyList<MatrixPetriNet> Streaming_LoadMatrix()
    {
        return streamingLoader.LoadMatrix(scenario.PnmlPath);
    }
}

#pragma warning restore CS0618
