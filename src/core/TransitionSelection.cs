namespace petrinets2.core;

public static class TransitionSelection
{
    public static int? SelectHighestPriority(IEnumerable<int> enabledTransitions, Func<int, int> getTransitionPriority)
    {
        ArgumentNullException.ThrowIfNull(enabledTransitions);
        ArgumentNullException.ThrowIfNull(getTransitionPriority);

        var found = false;
        var bestTransition = default(int);
        var bestPriority = int.MinValue;

        foreach (var transitionId in enabledTransitions)
        {
            var priority = getTransitionPriority(transitionId);
            if (!found || priority > bestPriority)
            {
                bestTransition = transitionId;
                bestPriority = priority;
                found = true;
            }
        }

        return found ? bestTransition : null;
    }
}