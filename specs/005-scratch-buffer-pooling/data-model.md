# Data Model: Scratch Buffer Pooling

## Entity: Scratch Buffer Lease

- Purpose: Exclusive ownership token for one acquired scratch buffer during one execution cycle.

### Fields

- `LeaseId`: Unique identifier for tracking acquisition and release.
- `OwnerThreadId`: Thread identity that owns the lease.
- `BufferKind`: Planning scratch or firing-delta scratch.
- `Capacity`: Current buffer capacity.
- `State`: Acquired, Released, or Faulted.

### Validation rules

- Lease transitions only from `Acquired -> Released` or `Acquired -> Faulted`.
- Released lease must not be used or released again.
- `OwnerThreadId` must match active thread for thread-local returns.

### Invariants

- A lease has exactly one successful release.
- No cross-call reuse occurs before release.
- Lease disposal cannot mutate external marking/planning outputs.

## Entity: Thread-Local Scratch Pool

- Purpose: Fast-path local reuse container for per-thread scratch buffers.

### Fields

- `ThreadId`
- `PlanningBuffers`
- `FiringDeltaBuffers`
- `RetainedCapacity`
- `CapacityCap`

### Validation rules

- `RetainedCapacity <= CapacityCap` after return operations.
- Buffers above cap are not retained locally.

### Invariants

- Local acquisition path requires no global lock for already-retained buffers.
- Local pool state is isolated per thread.

## Entity: Shared Fallback Pool

- Purpose: Overflow source when thread-local pool cannot satisfy acquisition.

### Fields

- `AvailableBuffersByKind`
- `WaitQueue` (implementation-dependent)
- `RetryPolicy` (5 retries, exponential backoff)

### Validation rules

- Returned buffer belongs to known pool kind.
- Exhaustion after retry budget throws explicit acquisition exception.

### Invariants

- Acquisition attempts are bounded by retry policy.
- Exhaustion path does not commit partial state updates.

## Entity: Pool Configuration

- Purpose: Internal tuning inputs for retained-capacity and retry behavior.

### Fields

- `PerThreadCapacityCap` (default 256 KiB retained capacity per buffer kind per thread; internal non-public override)
- `RetryCount` (=5)
- `BackoffMode` (Exponential)
- `PerThreadCapacityCapMin` (=64 KiB)
- `PerThreadCapacityCapMax` (=8 MiB)

### Validation rules

- Values must be positive and bounded to prevent invalid runtime configuration.
- Effective `PerThreadCapacityCap` must satisfy `PerThreadCapacityCapMin <= PerThreadCapacityCap <= PerThreadCapacityCapMax`.

### Invariants

- Configuration is internal and does not alter public API signatures.

## Entity: Execution Cycle Context

- Purpose: Encapsulates one planning or firing operation boundary using pooled scratch resources.

### Fields

- `CycleKind` (Planning or Firing)
- `InputMarking`
- `SelectedTransitions`
- `AcquiredLeases`
- `Result`

### Validation rules

- All acquired leases are released or faulted before cycle completion.

### Invariants

- For identical inputs, pooled and baseline outputs are equivalent.
- On acquisition failure, `Result` is failure-only with no partial mutation.

## Relationships

- `Execution Cycle Context` acquires `Scratch Buffer Lease` from `Thread-Local Scratch Pool` first, then `Shared Fallback Pool` on overflow.
- `Pool Configuration` constrains `Thread-Local Scratch Pool` retention and `Shared Fallback Pool` retries.
- Lease lifecycle invariants enforce FR-006 ownership semantics and FR-010 failure atomicity.
