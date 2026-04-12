namespace core.tests;

public class TransitionSelectionContractProperties
{
    [Property]
    public bool EmptyEnabledSet_ReturnsNull()
    {
        return TransitionSelection.SelectHighestPriority(Array.Empty<int>(), _ => 0) is null;
    }

    [Property]
    public bool MissingPriorityDefaultsToZero(NonNegativeInt explicitPrioritySeed)
    {
        var explicitPriority = explicitPrioritySeed.Get % 16;
        var priorities = new Dictionary<int, int> { [0] = explicitPriority };
        var selected = TransitionSelection.SelectHighestPriority(new[] { 0, 1 }, transitionId => priorities.TryGetValue(transitionId, out var priority) ? priority : 0);

        return selected == (explicitPriority >= 0 ? 0 : 1);
    }
}