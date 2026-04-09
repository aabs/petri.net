namespace petrinets2.core;

public class Marking 
{
    readonly int[] markingVector;

    public int Size { get; }

    public Marking(int size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative.");
        }
        if(size > GraphPetriNet.MaxSize) 
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Marking size exceeds MaxSize.");
        }

        markingVector = new int[size];
        Size = size;
    }

    public Marking(int[] vec)
    {
        ArgumentNullException.ThrowIfNull(vec);

        if (vec.Length > GraphPetriNet.MaxSize)
        {
            throw new ArgumentOutOfRangeException(nameof(vec), "Marking size exceeds MaxSize.");
        }

        markingVector = vec.ToArray();
        Size = vec.Length;
    }

    public Marking(int size,
                   IDictionary<int, int> markings) 
    {
        ArgumentNullException.ThrowIfNull(markings);

        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative.");
        }
        if (size > GraphPetriNet.MaxSize)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Marking size exceeds MaxSize.");
        }

        foreach (var key in markings.Keys)
        {
            if (key < 0 || key >= size)
            {
                throw new ArgumentOutOfRangeException(nameof(markings), "Marking key is out of range for the supplied size.");
            }
        }

        markingVector = new int[size];
        Size = size;

        foreach (var x in markings)
        {
            this[x.Key] = x.Value;
        }
    }

    public Marking(Marking m) 
    {
        ArgumentNullException.ThrowIfNull(m);

        markingVector = new int[m.markingVector.Length];
        Array.Copy(m.markingVector, markingVector, m.markingVector.Length);
        Size = m.Size;
    }

    public int this[int index]
    {
        get
        {
            if (index < 0 || index >= Size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return markingVector[index];
        }
        set
        {
            if (index < 0 || index >= Size)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            markingVector[index] = value;
        }
    }
}