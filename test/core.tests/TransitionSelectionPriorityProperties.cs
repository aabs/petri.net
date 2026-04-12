namespace core.tests;

public class TransitionSelectionPriorityProperties
{
    [Property]
    public bool HighestPriorityWins(NonNegativeInt leftPrioritySeed, NonNegativeInt rightPrioritySeed, NonNegativeInt thirdPrioritySeed)
    {
        var transitions = new[] { 0, 1, 2 };
        var priorities = new Dictionary<int, int>
        {
            [0] = leftPrioritySeed.Get % 16,
            [1] = rightPrioritySeed.Get % 16,
            [2] = thirdPrioritySeed.Get % 16
        };

        var selected = TransitionSelection.SelectHighestPriority(transitions, transitionId => priorities[transitionId]);
        var expected = transitions.OrderByDescending(transitionId => priorities[transitionId]).ThenBy(transitionId => transitionId).First();

        return selected == expected;
    }

    [Property]
    public bool FirstSeenTieWins(NonNegativeInt prioritySeed)
    {
        var priority = prioritySeed.Get % 16;
        var transitions = new[] { 4, 2, 7 };
        var selected = TransitionSelection.SelectHighestPriority(transitions, _ => priority);

        return selected == 4;
    }
}