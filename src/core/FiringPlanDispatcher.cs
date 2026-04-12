namespace petrinets2.core;

public static class FiringPlanDispatcher
{
    public static IReadOnlyList<int> GetDispatchOrder(FiringPlan firingPlan)
    {
        ArgumentNullException.ThrowIfNull(firingPlan);
        return firingPlan.TransitionIds;
    }

    public static int Dispatch(FiringPlan firingPlan, IReadOnlyDictionary<int, List<Action<int>>> transitionFunctions)
    {
        ArgumentNullException.ThrowIfNull(firingPlan);
        ArgumentNullException.ThrowIfNull(transitionFunctions);

        var dispatchedTransitions = 0;
        foreach (var transitionId in firingPlan.TransitionIds)
        {
            if (transitionFunctions.TryGetValue(transitionId, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler(transitionId);
                }
            }

            dispatchedTransitions++;
        }

        return dispatchedTransitions;
    }
}