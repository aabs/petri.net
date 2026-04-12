namespace petrinets2.core;

public sealed class FiringPlan
{
    static readonly FiringPlan EmptyInstance = new(Array.Empty<int>());
    readonly int[] transitionIds;

    FiringPlan(int[] transitionIds)
    {
        this.transitionIds = transitionIds;
    }

    public static FiringPlan Empty => EmptyInstance;

    public IReadOnlyList<int> TransitionIds => transitionIds;

    public int Count => transitionIds.Length;

    public bool IsEmpty => transitionIds.Length == 0;

    public static FiringPlan ForTransition(int transitionId)
    {
        if (transitionId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(transitionId));
        }

        return new FiringPlan(new[] { transitionId });
    }

    public static FiringPlan FromTransitions(IEnumerable<int> transitionIds)
    {
        ArgumentNullException.ThrowIfNull(transitionIds);

        var ids = transitionIds.ToArray();
        if (ids.Any(static id => id < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(transitionIds), "Transition ids must be non-negative.");
        }

        return ids.Length == 0 ? Empty : new FiringPlan(ids);
    }

    public bool Contains(int transitionId)
    {
        return Array.IndexOf(transitionIds, transitionId) >= 0;
    }
}