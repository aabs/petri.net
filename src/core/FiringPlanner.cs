namespace petrinets2.core;

public static class FiringPlanner
{
    public static FiringPlan Create(IEnumerable<int> enabledTransitions, bool isConflicted, Func<int, int> getTransitionPriority)
    {
        ArgumentNullException.ThrowIfNull(enabledTransitions);
        ArgumentNullException.ThrowIfNull(getTransitionPriority);

        var enabled = enabledTransitions.ToArray();
        if (enabled.Length == 0)
        {
            return FiringPlan.Empty;
        }

        if (!isConflicted)
        {
            return FiringPlan.FromTransitions(enabled);
        }

        var selectedTransition = TransitionSelection.SelectHighestPriority(enabled, getTransitionPriority);
        return selectedTransition.HasValue
            ? FiringPlan.ForTransition(selectedTransition.Value)
            : FiringPlan.Empty;
    }
}