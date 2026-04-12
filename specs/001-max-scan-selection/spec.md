# Feature Specification: Max-Scan Transition Selection

**Feature Branch**: `001-max-scan-selection`  
**Created**: 2026-04-12  
**Status**: Draft  
**Input**: User description: "Replace sort/LINQ-based transition priority selection with a single-pass max-scan in the hot execution path"

## Clarifications

### Session 2026-04-12

- Q: Should benchmark validation for this feature require committed microbenchmarks for both graph and matrix selection paths? → A: Yes, committed microbenchmarks for both `GraphPetriNet` and `MatrixPetriNet` selection paths are in scope for this feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Faster Transition Selection Under Conflict (Priority: P1)

When a Petri net has conflicting enabled transitions (multiple transitions competing for tokens in a shared place), the engine must select the highest-priority transition to fire. Currently this selection sorts all enabled transitions by priority. A single-pass max-scan should deliver the same result with less CPU work and zero allocations.

**Why this priority**: This is the core behavior change — the primary hot-path improvement that directly addresses the performance problem.

**Independent Test**: Can be fully tested by constructing a net with multiple enabled transitions of varying priorities under a conflicted marking, firing it, and verifying the same highest-priority transition is selected as the current implementation.

**Acceptance Scenarios**:

1. **Given** a Petri net with transitions T0 (priority 5), T1 (priority 10), T2 (priority 3) all enabled under marking M, **When** the engine selects the next transition to fire, **Then** T1 is selected (highest priority).
2. **Given** a Petri net with transitions T0 (priority 5), T1 (priority 5) both enabled under marking M, **When** the engine selects the next transition to fire, **Then** the transition with the lower index is selected (deterministic tie-breaking consistent with current behavior).
3. **Given** a Petri net where no transitions are enabled under marking M, **When** the engine selects the next transition to fire, **Then** null is returned and no exception is thrown.

---

### User Story 2 - Matrix Path Selection Parity (Priority: P1)

The `MatrixPetriNet.GetNextTransitionToFire` method must produce identical results to the current LINQ-based implementation after the max-scan replacement.

**Why this priority**: The matrix path is a primary execution path for large nets and must maintain exact behavioral parity.

**Independent Test**: Run the existing matrix Petri net test suite — all tests must pass without modification after the change.

**Acceptance Scenarios**:

1. **Given** a `MatrixPetriNet` with prioritized transitions, **When** `GetNextTransitionToFire` is called, **Then** the result matches the current sort-based selection for all existing test nets.
2. **Given** a `MatrixPetriNet` with flat (equal) priorities, **When** `GetNextTransitionToFire` is called, **Then** the first enabled transition by index is returned, matching current `FirstOrDefault` behavior on a stable sort.

---

### User Story 3 - Graph Path Selection Parity (Priority: P1)

The `GraphPetriNet.GetNextTransitionToFire` method must produce identical results to the current LINQ-based implementation after the max-scan replacement.

**Why this priority**: The graph path is the other primary execution path and must maintain exact behavioral parity.

**Independent Test**: Run the existing graph Petri net test suite — all tests must pass without modification after the change.

**Acceptance Scenarios**:

1. **Given** a `GraphPetriNet` with prioritized transitions, **When** `GetNextTransitionToFire` is called, **Then** the result matches the current sort-based selection for all existing test nets.
2. **Given** a `GraphPetriNet` with no enabled transitions, **When** `GetNextTransitionToFire` is called, **Then** null is returned.

---

### User Story 4 - Reduced CPU in Selection Path (Priority: P2)

Under benchmark conditions with large nets (many transitions and places), the new max-scan selection must demonstrate measurably lower CPU usage per call compared to the sort-based approach.

**Why this priority**: This is the driving motivation for the change but is a non-functional outcome verified by benchmark, not a behavioral change.

**Independent Test**: A microbenchmark targeting `GetNextTransitionToFire` on both graph and matrix paths with nets of varying sizes, comparing before/after CPU time.

**Acceptance Scenarios**:

1. **Given** a benchmark harness with a large Petri net (100+ transitions), **When** `GetNextTransitionToFire` is called repeatedly, **Then** CPU per call is at least 20% lower than the LINQ-based baseline.
2. **Given** a benchmark with allocation profiling, **When** `GetNextTransitionToFire` is called in steady state, **Then** zero additional heap allocations occur per call.
3. **Given** the feature branch is ready for validation, **When** benchmark evidence is reviewed, **Then** committed microbenchmarks exist for both `GraphPetriNet` and `MatrixPetriNet` selection paths and can be run to reproduce the reported before/after results.

---

### Edge Cases

- What happens when all transitions have the same priority? The lowest-index enabled transition should be returned (preserving current stable-sort-then-first semantics).
- What happens when only one transition is enabled? That transition is returned directly.
- What happens when the transition priority map is sparse (some transitions have no explicit priority)? Transitions without an explicit priority entry default to priority 0, matching current `GetTransitionPriority` behavior.
- What happens when the net has transitions but none are enabled? Null is returned with no side effects.
- What happens with a single-transition net where it is enabled? That transition is returned.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The max-scan selection MUST return the same transition as the current LINQ `orderby ... descending` + `FirstOrDefault`/`First` implementation for all possible markings and priority configurations.
- **FR-002**: When no transitions are enabled, `GetNextTransitionToFire` MUST return null (for `MatrixPetriNet`) or null (for `GraphPetriNet`), identical to current behavior.
- **FR-003**: When multiple enabled transitions share the highest priority, the one with the lowest transition index MUST be selected, preserving deterministic tie-breaking consistent with the current stable-sort behavior.
- **FR-004**: The max-scan logic MUST be implemented in both `GraphPetriNet` and `MatrixPetriNet` classes.
- **FR-005**: The public method signature of `GetNextTransitionToFire(Marking m)` MUST remain unchanged in both classes.
- **FR-006**: The `CreateFiringPlan` method in `MatrixPetriNet`, which calls `GetNextTransitionToFire` during conflict resolution, MUST continue to function correctly.
- **FR-007**: The `Fire` method in `GraphPetriNet`, which calls `GetNextTransitionToFire` for single-transition firing, MUST continue to function correctly.
- **FR-008**: The feature MUST include committed, runnable microbenchmarks that measure `GetNextTransitionToFire` on both `GraphPetriNet` and `MatrixPetriNet` using representative large-net inputs.

### Key Entities

- **Transition**: Identified by integer index, has an associated priority (default 0).
- **Marking**: A vector of token counts per place that determines which transitions are enabled.
- **Priority**: An integer value associated with each transition; higher values mean higher priority. Stored in `TransitionPriorities` dictionary.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All existing unit and property-based tests pass without modification after the change.
- **SC-002**: Transition selection completes at least 20% faster per call in benchmarked large nets (100+ transitions) compared to the LINQ-based baseline.
- **SC-003**: Zero additional heap allocations per `GetNextTransitionToFire` call in steady-state operation.
- **SC-004**: Deterministic tie-breaking behavior is preserved — identical transition is selected for identical inputs before and after the change.
- **SC-005**: Benchmark results are reproducible from committed benchmark code covering both graph and matrix selection paths.

## Assumptions

- The current LINQ `orderby ... descending` + `FirstOrDefault`/`First` behavior on equal-priority transitions selects the first one encountered in enumeration order, which corresponds to the lowest transition index from `GetEnabledTransitions`. The max-scan replacement will preserve this by preferring lower indices on tie.
- The `GetTransitionPriority` method's default-to-zero behavior for missing priority entries is unchanged and relied upon.
- The `GetEnabledTransitions` / `AllEnabledTransitions` methods are not modified by this feature — only the selection logic that consumes their output is changed.
- Benchmark infrastructure needed to validate the change is part of this feature's deliverable scope for both graph and matrix paths.
- The `IsConflicted` check in `CreateFiringPlan` that gates whether `GetNextTransitionToFire` is called remains unchanged.
