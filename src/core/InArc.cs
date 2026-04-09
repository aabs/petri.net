namespace petrinets2.core;

public record InArc(int Source, bool IsInhibitor) : Arc
{
    public InArc(int source, int weight = 1, bool inhibitor = false) : this(source, inhibitor)
    {
        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be greater than zero.");
        }

        Weight = weight;
    }
}