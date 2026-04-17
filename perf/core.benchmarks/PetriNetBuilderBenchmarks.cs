namespace core.benchmarks;

using BenchmarkDotNet.Attributes;
using petrinets2.core;

[MemoryDiagnoser]
public class PetriNetBuilderBenchmarks
{
    PetriNetBuilderBenchmarkScenario scenario = null!;

    [Params(256, 1024)]
    public int PlaceCount { get; set; }

    [Params(256, 1024)]
    public int TransitionCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        scenario = PetriNetBuilderBenchmarkScenarios.Create(PlaceCount, TransitionCount);
    }

    [Benchmark]
    public GraphPetriNet BuildGraph()
    {
        return PetriNetBuilderBenchmarkScenarios.BuildBuilder(scenario).BuildGraph();
    }

    [Benchmark]
    public MatrixPetriNet BuildMatrix()
    {
        return PetriNetBuilderBenchmarkScenarios.BuildBuilder(scenario).BuildMatrix();
    }
}
