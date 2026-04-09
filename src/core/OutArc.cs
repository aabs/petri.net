namespace petrinets2.core;

public record OutArc(int Target) : Arc
{
    public OutArc(int target, int weight) : this(target)
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than zero.");
        }

        Weight = weight;
    }
}