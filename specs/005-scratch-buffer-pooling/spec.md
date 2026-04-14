# Feature Specification: Scratch Buffer Pooling

**Feature Branch**: `005-scratch-buffer-pooling`  
**Created**: 2026-04-14  
**Status**: Draft  
**Input**: User description: "Reduce use of transient buffer allocations in favour of pooling"

## Clarifications

### Session 2026-04-14

- Q: Which scratch-buffer ownership model should be used under concurrency? → A: Thread-local scratch pools with shared fallback when needed.
- Q: What should happen when shared fallback cannot immediately provide a buffer? → A: Use bounded wait/retry, then fail with an explicit exception.
- Q: How should per-thread scratch pool growth be bounded? → A: Use a configurable per-thread cap with shared fallback.
- Q: How should the per-thread cap be configured? → A: Use an internal configurable setting with a documented default and optional advanced non-public override.
- Q: What retry policy should bounded acquisition use? → A: 5 retries with exponential backoff.

## Capability Scenarios & Testing *(mandatory)*

### Capability Slice 1 - Pooled Steady-State Scratch Reuse (Priority: P1)

**Capability**: The system reuses scratch working buffers across repeated planning and firing cycles so steady-state execution avoids repeated transient allocation for enabled-transition materialization and per-fire delta accumulation.

**Why this priority**: These allocations occur on every planning/fire cycle and are a direct source of sustained GC pressure.

**Independent Test**: Property-based workload families execute many repeated planning/fire cycles and assert the same planning/fire outcomes as baseline while tracking allocation-rate reduction under equivalent load.

**Acceptance Scenarios**:

1. **Given** a net under sustained repeated planning calls, **When** planning executes across many cycles, **Then** scratch working storage is reused and planning outcomes remain identical to baseline behavior.
2. **Given** a net under sustained repeated fire calls, **When** fire executes across many cycles, **Then** per-fire delta scratch storage is reused and resulting markings remain identical to baseline behavior.

---

### Capability Slice 2 - Contract-Safe Reuse Under Concurrency (Priority: P2)

**Capability**: Buffer pooling preserves thread safety, reentrancy expectations, and deterministic outcomes for identical inputs.

**Why this priority**: Incorrect buffer ownership or reuse can introduce race conditions and nondeterministic behavior.

**Independent Test**: Property-based concurrent execution families and repeated identical-input runs verify no collisions, no cross-call contamination, and deterministic outputs.

**Acceptance Scenarios**:

1. **Given** concurrent planning/fire activity, **When** pooled scratch buffers are acquired and released repeatedly, **Then** no shared-state corruption occurs and outputs match contract-defined behavior.
2. **Given** identical net state and transition inputs across repeated runs, **When** execution is repeated with pooling enabled, **Then** observable results are deterministic and equal to baseline.

---

### Capability Slice 3 - Scope-Bounded Allocation Reduction (Priority: P3)

**Capability**: Pooling work is applied to proven steady-state scratch allocation hotspots first, while preserving current behavior in non-targeted startup/load paths unless explicitly expanded.

**Why this priority**: The feature must deliver measurable benefit quickly without broad, high-risk refactors.

**Independent Test**: Targeted benchmark profiles compare baseline and pooled execution for steady-state planning/fire paths and confirm scope boundaries remain intact.

**Acceptance Scenarios**:

1. **Given** benchmark scenarios focused on repeated planning/fire cycles, **When** pooling is enabled for targeted scratch data, **Then** allocation-rate reduction is measurable and correctness remains unchanged.
2. **Given** startup/load workflows outside initial pooling scope, **When** the system runs unchanged workflows, **Then** behavior remains functionally equivalent to baseline.
3. **Given** internal default cap configuration and non-public override values, **When** per-thread retained-capacity bounds are evaluated, **Then** default behavior uses 256 KiB per buffer kind per thread and override bounds enforce 64 KiB minimum and 8 MiB maximum.

### Edge Cases

- Zero enabled transitions during planning.
- Empty delta updates during firing.
- Full affected-set updates where most places are touched.
- Reentrant or nested planning/fire invocation sequences.
- Buffer return after exceptions during execution.
- Shared fallback exhaustion after bounded retries must fail with an explicit exception and no partial state mutation.
- Retry-budget exhaustion after 5 exponential-backoff retries must fail with an explicit exception and no partial state mutation.
- Per-thread cap reached while nested/reentrant execution requests additional scratch leases.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST reduce repeated transient allocations in steady-state planning by reusing scratch working storage for enabled-transition handling.
- **FR-002**: System MUST reduce repeated transient allocations in steady-state firing by reusing scratch working storage for per-fire token-delta accumulation.
- **FR-003**: System MUST preserve externally observable planning and firing behavior parity with baseline for identical inputs.
- **FR-004**: System MUST preserve deterministic outcomes for repeated identical inputs.
- **FR-005**: System MUST preserve thread safety and reentrancy assumptions under pooled scratch reuse by defaulting to thread-local scratch pools.
- **FR-006**: System MUST enforce explicit scratch-buffer ownership and release semantics, using shared fallback buffers only when thread-local scratch capacity is insufficient, to prevent leak, double-return, or cross-call contamination.
- **FR-007**: System MUST keep scope bounded to proven steady-state hotspots unless expanded by a later specification.
- **FR-008**: System MUST define contract-sensitive behavior for inputs, outputs, invariants, and failure modes using idiomatic C# expectations rather than relying on deprecated Code Contracts tooling.
- **FR-009**: System MUST apply exactly 5 retries with exponential backoff on shared fallback buffer acquisition and then throw an explicit exception on exhaustion.
- **FR-010**: System MUST guarantee failure atomicity for acquisition exhaustion: no partial marking or planning-state mutation is observable when acquisition fails.
- **FR-011**: System MUST enforce a configurable per-thread scratch capacity cap and route overflow acquisitions through shared fallback behavior.
- **FR-012**: System MUST source per-thread cap configuration from an internal setting with a documented default (256 KiB retained capacity per buffer kind per thread) and optional advanced non-public override while preserving existing public API shape.

### Key Entities *(include if feature involves data)*

- **Scratch Buffer Lease**: A temporary ownership token that defines who can use a pooled scratch buffer and when it must be returned.
- **Planning Scratch Set**: Temporary state used during enabled-transition evaluation and selection.
- **Firing Delta Scratch Set**: Temporary state used to accumulate token-delta effects during fire execution.
- **Execution Cycle**: A single planning or firing operation boundary that must not leak scratch state across calls.
- **Pool Topology**: Thread-local scratch pools with shared fallback for overflow/pressure conditions.
- **Per-Thread Capacity Cap**: A configurable upper bound on retained scratch capacity per thread before overflow uses shared fallback; default is 256 KiB retained capacity per buffer kind per thread with internal override bounds of 64 KiB minimum and 8 MiB maximum.

### Scenario to Requirement Traceability *(mandatory)*

- **Capability Slice 1**: FR-001, FR-002, FR-003
- **Capability Slice 2**: FR-003, FR-004, FR-005, FR-006
- **Capability Slice 3**: FR-003, FR-007, FR-008, FR-009, FR-010, FR-011

For each FR, list at least one acceptance scenario that proves it:
- **FR-001 proven by**: Capability Slice 1, Scenario 1
- **FR-002 proven by**: Capability Slice 1, Scenario 2
- **FR-003 proven by**: Capability Slice 1, Scenario 1; Capability Slice 1, Scenario 2; Capability Slice 3, Scenario 2
- **FR-004 proven by**: Capability Slice 2, Scenario 2
- **FR-005 proven by**: Capability Slice 2, Scenario 1
- **FR-006 proven by**: Capability Slice 2, Scenario 1
- **FR-007 proven by**: Capability Slice 3, Scenario 2
- **FR-008 proven by**: Capability Slice 3, Scenario 2
- **FR-009 proven by**: Capability Slice 3, Scenario 2
- **FR-010 proven by**: Capability Slice 3, Scenario 2
- **FR-011 proven by**: Capability Slice 2, Scenario 1; Capability Slice 3, Scenario 1
- **FR-012 proven by**: Capability Slice 3, Scenario 3

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Property-based parity validation for targeted planning/fire workloads passes at 100% across agreed randomized families.
- **SC-002**: Allocation rate for targeted steady-state planning/fire workloads is reduced by at least 20% versus baseline under equivalent benchmark conditions.
- **SC-003**: Throughput for targeted steady-state planning/fire workloads shows no regression greater than 5% versus baseline.
- **SC-004**: Determinism checks for repeated identical inputs pass at 100% across benchmark and property-test replay runs.
- **SC-005**: Concurrency stress validation reports zero scratch ownership violations (leak, double-return, or cross-call contamination).

When performance is a goal, success criteria MUST state correctness-preserving boundaries first, then measurable performance targets and validation method.

## Assumptions

- Existing benchmark and property-test harnesses are available to compare baseline and pooled behavior under equivalent workloads.
- The highest-value initial scope is steady-state planning/fire allocations rather than startup/load allocation churn.
- Public APIs and externally observable firing semantics remain unchanged for this feature.
- Per-thread cap tuning is internal by default; any override mechanism remains non-public in this feature scope.
- Default per-thread retained capacity cap is 256 KiB per buffer kind per thread (override bounds: 64 KiB to 8 MiB, internal only).
- Retry behavior implementation remains internal and does not change the public API surface.
- Any expansion into non-steady-state allocation hotspots will be handled in a follow-up specification after measured validation.
