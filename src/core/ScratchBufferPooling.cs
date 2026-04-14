using System.Collections.Concurrent;
using System.Threading;

namespace petrinets2.core;

internal enum ScratchBufferKind
{
    Planning,
    FiringDelta
}

internal enum ScratchLeaseState
{
    Acquired,
    Faulted,
    Released
}

internal readonly record struct ScratchPoolConfiguration(int PerThreadCapacityCapBytes, int RetryCount)
{
    public const int DefaultPerThreadCapacityCapBytes = 256 * 1024;
    public const int MinPerThreadCapacityCapBytes = 64 * 1024;
    public const int MaxPerThreadCapacityCapBytes = 8 * 1024 * 1024;

    public static ScratchPoolConfiguration Default => new(DefaultPerThreadCapacityCapBytes, 5);

    public static int ClampPerThreadCapacity(int requested)
    {
        if (requested < MinPerThreadCapacityCapBytes)
        {
            return MinPerThreadCapacityCapBytes;
        }

        if (requested > MaxPerThreadCapacityCapBytes)
        {
            return MaxPerThreadCapacityCapBytes;
        }

        return requested;
    }
}

internal readonly record struct ScratchBufferPoolingDiagnostics(
    long PlanningRentals,
    long DeltaRentals,
    long PlanningFreshAllocations,
    long DeltaFreshAllocations,
    long RetryAttempts,
    long AcquisitionFailures,
    long ReleaseViolations,
    long DoubleReturnViolations,
    long PlanningOutstandingLeases,
    long DeltaOutstandingLeases,
    int EffectivePerThreadCapacityCapBytes,
    int RetryCount);

internal sealed class ScratchBufferAcquisitionException : InvalidOperationException
{
    public ScratchBufferAcquisitionException(string message)
        : base(message)
    {
    }
}

internal static class ScratchBufferPooling
{
    static readonly ThreadLocal<Stack<List<int>>> PlanningLocalPool = new(static () => new Stack<List<int>>());
    static readonly ThreadLocal<Stack<Dictionary<int, int>>> DeltaLocalPool = new(static () => new Stack<Dictionary<int, int>>());
    static readonly ConcurrentQueue<List<int>> PlanningSharedPool = new();
    static readonly ConcurrentQueue<Dictionary<int, int>> DeltaSharedPool = new();

    static ScratchPoolConfiguration config = ScratchPoolConfiguration.Default;
    static volatile bool poolingEnabled = true;
    static volatile bool forceAcquisitionExhaustion;

    static long planningRentals;
    static long deltaRentals;
    static long planningFreshAllocations;
    static long deltaFreshAllocations;
    static long retryAttempts;
    static long acquisitionFailures;
    static long releaseViolations;
    static long doubleReturnViolations;
    static long planningOutstandingLeases;
    static long deltaOutstandingLeases;

    internal static int EffectivePerThreadCapacityCapBytes => config.PerThreadCapacityCapBytes;

    internal static int RetryCount => config.RetryCount;

    internal static PlanningScratchLease AcquirePlanningScratch()
    {
        Interlocked.Increment(ref planningRentals);
        Interlocked.Increment(ref planningOutstandingLeases);

        var buffer = AcquirePlanningBuffer();
        return new PlanningScratchLease(buffer, Environment.CurrentManagedThreadId);
    }

    internal static DeltaScratchLease AcquireDeltaScratch()
    {
        Interlocked.Increment(ref deltaRentals);
        Interlocked.Increment(ref deltaOutstandingLeases);

        var buffer = AcquireDeltaBuffer();
        return new DeltaScratchLease(buffer, Environment.CurrentManagedThreadId);
    }

    internal static ScratchBufferPoolingDiagnostics GetDiagnosticsSnapshot()
    {
        return new ScratchBufferPoolingDiagnostics(
            Interlocked.Read(ref planningRentals),
            Interlocked.Read(ref deltaRentals),
            Interlocked.Read(ref planningFreshAllocations),
            Interlocked.Read(ref deltaFreshAllocations),
            Interlocked.Read(ref retryAttempts),
            Interlocked.Read(ref acquisitionFailures),
            Interlocked.Read(ref releaseViolations),
            Interlocked.Read(ref doubleReturnViolations),
            Interlocked.Read(ref planningOutstandingLeases),
            Interlocked.Read(ref deltaOutstandingLeases),
            config.PerThreadCapacityCapBytes,
            config.RetryCount);
    }

    internal static void ResetDiagnostics()
    {
        Interlocked.Exchange(ref planningRentals, 0);
        Interlocked.Exchange(ref deltaRentals, 0);
        Interlocked.Exchange(ref planningFreshAllocations, 0);
        Interlocked.Exchange(ref deltaFreshAllocations, 0);
        Interlocked.Exchange(ref retryAttempts, 0);
        Interlocked.Exchange(ref acquisitionFailures, 0);
        Interlocked.Exchange(ref releaseViolations, 0);
        Interlocked.Exchange(ref doubleReturnViolations, 0);
        Interlocked.Exchange(ref planningOutstandingLeases, 0);
        Interlocked.Exchange(ref deltaOutstandingLeases, 0);
    }

    internal static IDisposable BeginTestScope(bool enablePooling, bool forceExhaustion = false, int? perThreadCapacityCapBytes = null, int? retryCount = null)
    {
        var previousEnabled = poolingEnabled;
        var previousForceExhaustion = forceAcquisitionExhaustion;
        var previousConfig = config;

        poolingEnabled = enablePooling;
        forceAcquisitionExhaustion = forceExhaustion;

        var requestedCap = perThreadCapacityCapBytes.HasValue
            ? ScratchPoolConfiguration.ClampPerThreadCapacity(perThreadCapacityCapBytes.Value)
            : previousConfig.PerThreadCapacityCapBytes;
        var requestedRetryCount = retryCount.GetValueOrDefault(previousConfig.RetryCount);
        if (requestedRetryCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retryCount), "Retry count must be greater than zero.");
        }

        config = new ScratchPoolConfiguration(requestedCap, requestedRetryCount);

        return new ScratchBufferTestScope(
            previousEnabled,
            previousForceExhaustion,
            previousConfig);
    }

    internal static void MarkReleaseViolation()
    {
        Interlocked.Increment(ref releaseViolations);
    }

    internal static void MarkDoubleReturnViolation()
    {
        Interlocked.Increment(ref doubleReturnViolations);
    }

    static List<int> AcquirePlanningBuffer()
    {
        if (!poolingEnabled)
        {
            Interlocked.Increment(ref planningFreshAllocations);
            return new List<int>();
        }

        if (forceAcquisitionExhaustion)
        {
            return AcquireFromSharedPool(
                ScratchBufferKind.Planning,
                PlanningSharedPool,
                ref planningFreshAllocations);
        }

        if (PlanningLocalPool.Value is { Count: > 0 } localPool)
        {
            return localPool.Pop();
        }

        return AcquireFromSharedPool(
            ScratchBufferKind.Planning,
            PlanningSharedPool,
            ref planningFreshAllocations);
    }

    static Dictionary<int, int> AcquireDeltaBuffer()
    {
        if (!poolingEnabled)
        {
            Interlocked.Increment(ref deltaFreshAllocations);
            return new Dictionary<int, int>();
        }

        if (forceAcquisitionExhaustion)
        {
            return AcquireFromSharedPool(
                ScratchBufferKind.FiringDelta,
                DeltaSharedPool,
                ref deltaFreshAllocations);
        }

        if (DeltaLocalPool.Value is { Count: > 0 } localPool)
        {
            return localPool.Pop();
        }

        return AcquireFromSharedPool(
            ScratchBufferKind.FiringDelta,
            DeltaSharedPool,
            ref deltaFreshAllocations);
    }

    static TBuffer AcquireFromSharedPool<TBuffer>(
        ScratchBufferKind kind,
        ConcurrentQueue<TBuffer> sharedPool,
        ref long freshAllocationCounter)
        where TBuffer : class, new()
    {
        if (forceAcquisitionExhaustion)
        {
            for (var attempt = 0; attempt < config.RetryCount; attempt++)
            {
                Interlocked.Increment(ref retryAttempts);
                Thread.SpinWait(1 << (attempt + 1));
            }

            Interlocked.Increment(ref acquisitionFailures);
            throw new ScratchBufferAcquisitionException($"Unable to acquire {kind} scratch buffer after {config.RetryCount} retries.");
        }

        for (var attempt = 0; attempt < config.RetryCount; attempt++)
        {
            if (sharedPool.TryDequeue(out var borrowed))
            {
                return borrowed;
            }

            Interlocked.Increment(ref retryAttempts);
            Thread.SpinWait(1 << (attempt + 1));
        }

        Interlocked.Increment(ref freshAllocationCounter);
        return new TBuffer();
    }

    static int EstimatePlanningBytes(List<int> buffer)
    {
        return sizeof(int) * buffer.Capacity;
    }

    static int EstimateDeltaBytes(Dictionary<int, int> buffer)
    {
        // Conservative estimate based on key/value storage overhead.
        return sizeof(int) * 4 * buffer.Count;
    }

    internal static void ReturnPlanningBuffer(List<int> buffer, int ownerThreadId, bool wasFaulted)
    {
        Interlocked.Decrement(ref planningOutstandingLeases);

        if (wasFaulted)
        {
            return;
        }

        buffer.Clear();
        if (!poolingEnabled)
        {
            return;
        }

        if (ownerThreadId == Environment.CurrentManagedThreadId
            && EstimatePlanningBytes(buffer) <= config.PerThreadCapacityCapBytes)
        {
            PlanningLocalPool.Value!.Push(buffer);
            return;
        }

        PlanningSharedPool.Enqueue(buffer);
    }

    internal static void ReturnDeltaBuffer(Dictionary<int, int> buffer, int ownerThreadId, bool wasFaulted)
    {
        Interlocked.Decrement(ref deltaOutstandingLeases);

        if (wasFaulted)
        {
            return;
        }

        buffer.Clear();
        if (!poolingEnabled)
        {
            return;
        }

        if (ownerThreadId == Environment.CurrentManagedThreadId
            && EstimateDeltaBytes(buffer) <= config.PerThreadCapacityCapBytes)
        {
            DeltaLocalPool.Value!.Push(buffer);
            return;
        }

        DeltaSharedPool.Enqueue(buffer);
    }

    sealed class ScratchBufferTestScope(
        bool previousEnabled,
        bool previousForceExhaustion,
        ScratchPoolConfiguration previousConfig) : IDisposable
    {
        int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            poolingEnabled = previousEnabled;
            forceAcquisitionExhaustion = previousForceExhaustion;
            config = previousConfig;
        }
    }
}

internal sealed class PlanningScratchLease : IDisposable
{
    readonly int ownerThreadId;
    readonly List<int> buffer;
    int state = (int)ScratchLeaseState.Acquired;

    internal PlanningScratchLease(List<int> buffer, int ownerThreadId)
    {
        this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        this.ownerThreadId = ownerThreadId;
    }

    public List<int> Buffer
    {
        get
        {
            if ((ScratchLeaseState)Volatile.Read(ref state) != ScratchLeaseState.Acquired)
            {
                ScratchBufferPooling.MarkReleaseViolation();
                throw new InvalidOperationException("Planning scratch lease cannot be used after fault or release.");
            }

            return buffer;
        }
    }

    public void MarkFaulted()
    {
        var previous = (ScratchLeaseState)Interlocked.CompareExchange(ref state, (int)ScratchLeaseState.Faulted, (int)ScratchLeaseState.Acquired);
        if (previous == ScratchLeaseState.Released)
        {
            ScratchBufferPooling.MarkReleaseViolation();
            throw new InvalidOperationException("Planning scratch lease cannot be faulted after release.");
        }
    }

    public void Dispose()
    {
        var previous = (ScratchLeaseState)Interlocked.Exchange(ref state, (int)ScratchLeaseState.Released);
        if (previous == ScratchLeaseState.Released)
        {
            ScratchBufferPooling.MarkDoubleReturnViolation();
            throw new InvalidOperationException("Planning scratch lease can only be released once.");
        }

        ScratchBufferPooling.ReturnPlanningBuffer(buffer, ownerThreadId, previous == ScratchLeaseState.Faulted);
    }
}

internal sealed class DeltaScratchLease : IDisposable
{
    readonly int ownerThreadId;
    readonly Dictionary<int, int> buffer;
    int state = (int)ScratchLeaseState.Acquired;

    internal DeltaScratchLease(Dictionary<int, int> buffer, int ownerThreadId)
    {
        this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        this.ownerThreadId = ownerThreadId;
    }

    public Dictionary<int, int> Buffer
    {
        get
        {
            if ((ScratchLeaseState)Volatile.Read(ref state) != ScratchLeaseState.Acquired)
            {
                ScratchBufferPooling.MarkReleaseViolation();
                throw new InvalidOperationException("Delta scratch lease cannot be used after fault or release.");
            }

            return buffer;
        }
    }

    public void MarkFaulted()
    {
        var previous = (ScratchLeaseState)Interlocked.CompareExchange(ref state, (int)ScratchLeaseState.Faulted, (int)ScratchLeaseState.Acquired);
        if (previous == ScratchLeaseState.Released)
        {
            ScratchBufferPooling.MarkReleaseViolation();
            throw new InvalidOperationException("Delta scratch lease cannot be faulted after release.");
        }
    }

    public void Dispose()
    {
        var previous = (ScratchLeaseState)Interlocked.Exchange(ref state, (int)ScratchLeaseState.Released);
        if (previous == ScratchLeaseState.Released)
        {
            ScratchBufferPooling.MarkDoubleReturnViolation();
            throw new InvalidOperationException("Delta scratch lease can only be released once.");
        }

        ScratchBufferPooling.ReturnDeltaBuffer(buffer, ownerThreadId, previous == ScratchLeaseState.Faulted);
    }
}
