using FsCheck;
using FsCheck.Xunit;
using petrinets2.core;
using Xunit;

namespace core.tests;

public class MarkingProperties
{
    [Property]
    public bool Constructor_FromArray_UsesArrayLengthAsSize(NonNull<int[]> source)
    {
        var values = source.Item;
        if (values.Length > GraphPetriNet.MaxSize)
        {
            values = values.Take(GraphPetriNet.MaxSize).ToArray();
        }

        var marking = new Marking(values);
        return marking.Size == values.Length;
    }

    [Property]
    public bool Constructor_FromArray_DefensivelyCopiesInput(NonNull<int[]> source)
    {
        var values = source.Item;
        if (values.Length == 0)
        {
            values = [0];
        }
        if (values.Length > GraphPetriNet.MaxSize)
        {
            values = values.Take(GraphPetriNet.MaxSize).ToArray();
        }

        var marking = new Marking(values);
        var previous = marking[0];
        values[0] = unchecked(values[0] + 1);

        return marking[0] == previous;
    }

    [Property]
    public bool CopyConstructor_CreatesIndependentClone(NonNull<int[]> source)
    {
        var values = source.Item;
        if (values.Length == 0)
        {
            values = [0];
        }
        if (values.Length > GraphPetriNet.MaxSize)
        {
            values = values.Take(GraphPetriNet.MaxSize).ToArray();
        }

        var original = new Marking(values);
        var copy = new Marking(original);

        var previous = copy[0];
        original[0] = unchecked(previous + 1);

        return copy[0] == previous && original[0] != copy[0] && copy.Size == original.Size;
    }

    [Property]
    public bool Indexer_RoundTripsValue(NonNegativeInt seed, int value)
    {
        var size = (seed.Get % 64) + 1;
        var index = Math.Abs(value % size);

        var marking = new Marking(size);
        marking[index] = value;

        return marking[index] == value;
    }

    [Property]
    public bool Constructor_InvalidSizes_ThrowArgumentOutOfRangeException(bool useNegative)
    {
        if (useNegative)
        {
            return Throws<ArgumentOutOfRangeException>(() => _ = new Marking(-1));
        }

        return Throws<ArgumentOutOfRangeException>(() => _ = new Marking(GraphPetriNet.MaxSize + 1));
    }

    [Fact]
    public void Constructor_NullArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new Marking((int[])null!));
    }

    [Fact]
    public void Indexer_OutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var marking = new Marking(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = marking[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = marking[1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => marking[1] = 10);
    }

    [Fact]
    public void Constructor_FromSize_InitializesAllSlotsToZero()
    {
        const int size = 8;
        var marking = new Marking(size);

        Assert.Equal(size, marking.Size);
        for (var i = 0; i < size; i++)
        {
            Assert.Equal(0, marking[i]);
        }
    }

    static bool Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
            return false;
        }
        catch (T)
        {
            return true;
        }
    }
}
