using System.Linq;

namespace core.tests;

public class TestNewPnmlLoaderMatrix
{
    [Fact]
    public void TestLoad1()
    {
        var loader = new NewPnmlLoader<MatrixPetriNet>();
        var path = PnmlLoaderDataGenerator.ExtractEmbeddedPnml("p0).xml");

        var nets = loader.Load(path).ToList();

        Assert.Single(nets);
    }

    [Theory]
    [MemberData(nameof(PnmlLoaderDataGenerator.FullSpecs), MemberType = typeof(PnmlLoaderDataGenerator))]
    public void TestLoadPnmlFile_AndRun(string path, IEnumerable<string> markingProgression)
    {
        var loader = new NewPnmlLoader<MatrixPetriNet>();
        var nets = loader.Load(path).ToList();

        Assert.Single(nets);

        var net = nets[0];
        var marking = net.CreateInitialMarking();

        for (var step = 0; step < 3; step++)
        {
            var enabled = net.GetEnabledTransitions(marking).ToArray();
            if (enabled.Length == 0)
            {
                break;
            }

            var next = net.Fire(marking);
            Assert.Equal(marking.Size, next.Size);
            for (var i = 0; i < next.Size; i++)
            {
                Assert.True(next[i] >= 0);
            }

            marking = next;
        }

        foreach (var _ in markingProgression)
        {
            // Placeholder for historical marking progression checks.
        }
    }
}
