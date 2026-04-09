using FsCheck;
using FsCheck.Xunit;
using petrinets2.core;

namespace core.tests;

public class PetriNetBaseProperties
{
    [Property]
    public bool Transition_IsEnabled_WhenTokenCountMeetsWeight(NonNegativeInt tokenCountSeed, PositiveInt weightSeed)
    {
        var tokens = tokenCountSeed.Get % 16;
        var weight = (weightSeed.Get % 8) + 1;

        var net = SingleTransitionNet.WithWeight(weight);
        var marking = new Marking(1);
        marking[0] = tokens;

        return net.IsEnabled(0, marking) == (tokens >= weight);
    }

    [Property]
    public bool Transition_IsDisabled_WhenInhibitorPlaceHasTokens(PositiveInt tokensSeed)
    {
        var net = SingleTransitionNet.WithInhibitor();
        var marking = new Marking(1);
        marking[0] = (tokensSeed.Get % 8) + 1;

        return !net.IsEnabled(0, marking);
    }

    private sealed class SingleTransitionNet : PetriNetBase
    {
        private readonly int _weight;
        private readonly bool _inhibitor;

        private SingleTransitionNet(int weight, bool inhibitor)
        {
            _weight = weight;
            _inhibitor = inhibitor;
        }

        public static SingleTransitionNet WithWeight(int weight)
        {
            return new SingleTransitionNet(weight, false);
        }

        public static SingleTransitionNet WithInhibitor()
        {
            return new SingleTransitionNet(1, true);
        }

        public override IEnumerable<int> AllPlaces()
        {
            return [0];
        }

        public override IEnumerable<int> InhibitorsIntoTransition(int transitionId)
        {
            return _inhibitor ? [0] : [];
        }

        public override IEnumerable<int> NonInhibitorsIntoTransition(int transitionId)
        {
            return _inhibitor ? [] : [0];
        }

        public override int GetWeight(int placeid, int transid)
        {
            return _weight;
        }

        public override IEnumerable<int> GetPlaceOutArcs(int placeId)
        {
            return [0];
        }

        public override bool IsEmptyTransition(int transitionId)
        {
            return false;
        }
    }
}
