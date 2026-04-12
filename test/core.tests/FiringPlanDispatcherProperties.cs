namespace core.tests;

public class FiringPlanDispatcherProperties
{
    [Property]
    public bool Dispatcher_InvokesHandlersInPlanOrder(NonNull<int[]> transitionIdsRaw)
    {
        var transitionIds = transitionIdsRaw.Item
            .Select(Math.Abs)
            .Select(value => value % 16)
            .Distinct()
            .Take(6)
            .ToArray();

        var plan = FiringPlan.FromTransitions(transitionIds);
        var calls = new List<int>();
        var handlers = transitionIds.ToDictionary(
            transitionId => transitionId,
            transitionId => new List<Action<int>> { id => calls.Add(id) });

        var dispatchCount = FiringPlanDispatcher.Dispatch(plan, handlers);

        return dispatchCount == transitionIds.Length
            && calls.SequenceEqual(transitionIds)
            && FiringPlanDispatcher.GetDispatchOrder(plan).SequenceEqual(transitionIds);
    }
}