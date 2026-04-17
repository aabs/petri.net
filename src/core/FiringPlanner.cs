namespace petrinets2.core;

public static class FiringPlanner
{
    public static FiringPlan Create(IEnumerable<int> enabledTransitions, bool isConflicted, Func<int, int> getTransitionPriority)
    {
        ArgumentNullException.ThrowIfNull(enabledTransitions);
        ArgumentNullException.ThrowIfNull(getTransitionPriority);

        using var planningScratchLease = ScratchBufferPooling.AcquirePlanningScratch();
        try
        {
            var enabled = planningScratchLease.Buffer;
            foreach (var transitionId in enabledTransitions)
            {
                enabled.Add(transitionId);
            }

            if (enabled.Count == 0)
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
        catch
        {
            planningScratchLease.MarkFaulted();
            throw;
        }
    }
}