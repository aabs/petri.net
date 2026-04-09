using FsCheck;
using FsCheck.Xunit;
using petrinets2.core;
using Xunit;

namespace core.tests;

public class MarkingProperties
{
    [Property]
    public bool SequenceOfWrites_MatchesArrayModel(NonNull<int[]> initialRaw, NonNull<int[]> indexRaw, NonNull<int[]> valueRaw)
    {
        var size = initialRaw.Item.Length == 0 ? 1 : Math.Min(initialRaw.Item.Length, 64);

        var expected = new int[size];
        var marking = new Marking(size);

        for (var i = 0; i < size; i++)
        {
            var v = initialRaw.Item.Length == 0 ? 0 : initialRaw.Item[i % initialRaw.Item.Length];
            expected[i] = v;
            marking[i] = v;
        }

        var steps = Math.Min(indexRaw.Item.Length, valueRaw.Item.Length);
        for (var i = 0; i < steps; i++)
        {
            var idxRaw = indexRaw.Item[i];
            var idx = idxRaw == int.MinValue ? 0 : Math.Abs(idxRaw) % size;
            var val = valueRaw.Item[i];

            expected[idx] = val;
            marking[idx] = val;
        }

        for (var i = 0; i < size; i++)
        {
            if (marking[i] != expected[i])
            {
                return false;
            }
        }

        return true;
    }

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
    public bool CopyConstructor_IsIdempotentForObservation(NonNull<int[]> source)
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

        var m1 = new Marking(values);
        var m2 = new Marking(m1);
        var m3 = new Marking(m2);

        if (m2.Size != m3.Size)
        {
            return false;
        }

        for (var i = 0; i < m2.Size; i++)
        {
            if (m2[i] != m3[i])
            {
                return false;
            }
        }

        return true;
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
    public bool Writes_AffectOnlyTargetIndex(PositiveInt sizeSeed, int iSeed, int jSeed, int value)
    {
        var size = (sizeSeed.Get % 64) + 1;
        var i = iSeed == int.MinValue ? 0 : Math.Abs(iSeed) % size;
        var j = jSeed == int.MinValue ? 0 : Math.Abs(jSeed) % size;

        var marking = new Marking(size);
        var before = new int[size];
        for (var x = 0; x < size; x++)
        {
            before[x] = marking[x];
        }

        marking[i] = value;

        if (i == j)
        {
            return marking[j] == value;
        }

        return marking[j] == before[j];
    }

    [Property]
    public bool Constructors_FromSparseMapAndWrites_AreEquivalent(PositiveInt sizeSeed, NonNull<int[]> keyRaw, NonNull<int[]> valueRaw)
    {
        var size = (sizeSeed.Get % 64) + 1;
        var map = new Dictionary<int, int>();
        var steps = Math.Min(keyRaw.Item.Length, valueRaw.Item.Length);

        for (var i = 0; i < steps; i++)
        {
            var kRaw = keyRaw.Item[i];
            var k = kRaw == int.MinValue ? 0 : Math.Abs(kRaw) % size;
            map[k] = valueRaw.Item[i];
        }

        var fromMap = new Marking(size, map);
        var fromWrites = new Marking(size);
        foreach (var pair in map)
        {
            fromWrites[pair.Key] = pair.Value;
        }

        for (var i = 0; i < size; i++)
        {
            if (fromMap[i] != fromWrites[i])
            {
                return false;
            }
        }

        return true;
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

    [Property]
    public bool Indexer_OutOfRangeIndices_AlwaysThrow(PositiveInt sizeSeed, int indexSeed)
    {
        var size = (sizeSeed.Get % 64) + 1;
        var marking = new Marking(size);

        var outOfRangeIndex = (indexSeed & 1) == 0
            ? -((Math.Abs(indexSeed == int.MinValue ? 1 : indexSeed) % 64) + 1)
            : size + (Math.Abs(indexSeed == int.MinValue ? 1 : indexSeed) % 64);

        return Throws<ArgumentOutOfRangeException>(() => _ = marking[outOfRangeIndex])
               && Throws<ArgumentOutOfRangeException>(() => marking[outOfRangeIndex] = 42);
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
