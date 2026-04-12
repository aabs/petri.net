# Feature Specification: Max-Scan Transition Selection

**Feature Branch**: `001-max-scan-selection`  
**Created**: 2026-04-12  
**Status**: Draft  
**Input**: User description: "Replace sort/LINQ-based transition priority selection with a single-pass max-scan in the hot execution path"

## Clarifications

### Session 2026-04-12

- Q: Should benchmark validation for this feature require committed microbenchmarks for both graph and matrix selection paths? → A: Yes, committed microbenchmarks for both `GraphPetriNet` and `MatrixPetriNet` selection paths are in scope for this feature.

### Session 2026-04-12

- Q: Should graph and matrix models be brought into alignment through a shared firing plan abstraction before subsequent performance work? → A: Yes, both models must use the same firing plan abstraction, and that alignment is foundational work that precedes subsequent performance-oriented changes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Shared Firing Plan Semantics (Priority: P1)

Both `GraphPetriNet` and `MatrixPetriNet` must derive transition execution from the same firing plan abstraction and hand that plan to the same dispatcher so that equivalent nets can be checked for behavioral equivalence and so later performance work optimizes one semantic model rather than two diverging ones.

**Why this priority**: This is foundational correctness work. Without a shared firing plan abstraction and a shared dispatch path, the two models cannot be checked for step-level equivalence and later optimizations risk amplifying model drift.

**Independent Test**: Can be fully tested by constructing equivalent graph and matrix nets, deriving a firing plan under the same marking, dispatching that plan through the same transition-handler actioning model, and verifying that both models produce the same plan shape and the same post-fire marking.

**Acceptance Scenarios**:

1. **Given** equivalent graph and matrix nets under the same marking, **When** each model derives a firing plan, **Then** both plans identify the same transitions to fire.
2. **Given** equivalent graph and matrix nets under the same marking, **When** each model dispatches the derived firing plan through the shared transition-handler actioning model, **Then** the same transition handlers are invoked in the same logical step.
3. **Given** equivalent graph and matrix nets under the same marking, **When** each model fires using the shared firing plan abstraction and dispatcher, **Then** both produce the same resulting marking.
4. **Given** a marking with no enabled transitions, **When** each model derives a firing plan, **Then** both return an empty plan and dispatching or firing leaves the marking unchanged.

---

### User Story 2 - Faster Transition Selection Under Conflict (Priority: P1)

When a Petri net has conflicting enabled transitions (multiple transitions competing for tokens in a shared place), the engine must select the highest-priority transition to fire. Currently this selection sorts all enabled transitions by priority. A single-pass max-scan should deliver the same result with less CPU work and zero allocations.

**Why this priority**: This is the core hot-path optimization, but it must now optimize the shared firing plan abstraction rather than one model-specific path.

**Independent Test**: Can be fully tested by constructing generated conflicting markings and verifying that the shared firing plan selects the same highest-priority transition as the current implementation.

**Acceptance Scenarios**:

1. **Given** a Petri net with transitions T0 (priority 5), T1 (priority 10), T2 (priority 3) all enabled under marking M, **When** the engine selects the next transition inside the firing plan, **Then** T1 is selected.
2. **Given** a Petri net with transitions T0 (priority 5), T1 (priority 5) both enabled under marking M, **When** the engine selects the next transition inside the firing plan, **Then** the transition with the lower index is selected.
3. **Given** a Petri net where no transitions are enabled under marking M, **When** the engine derives the firing plan, **Then** the plan is empty and `GetNextTransitionToFire` remains null.

---

### User Story 3 - Matrix Path Selection Parity (Priority: P1)

The matrix-backed implementation must preserve its observable selection and firing-plan behavior after adopting the shared firing plan abstraction and max-scan selection.

**Why this priority**: The matrix path is a primary execution path for large nets and must maintain exact behavioral parity after alignment.

**Independent Test**: Run the matrix property suite and verify parity for highest-priority selection, tie handling, null behavior, and conflict-driven firing-plan behavior.

**Acceptance Scenarios**:

1. **Given** a `MatrixPetriNet` with prioritized transitions, **When** the shared firing plan is derived, **Then** the selected transition matches the current sort-based selection.
2. **Given** a `MatrixPetriNet` with flat priorities, **When** the shared firing plan is derived, **Then** the first enabled transition by index is selected.

---

### User Story 4 - Graph Path Selection Parity (Priority: P1)

The graph-backed implementation must preserve its observable selection and firing behavior after adopting the shared firing plan abstraction and max-scan selection.

**Why this priority**: The graph path is the other primary execution path and must be aligned with the shared step semantics before further optimization.

**Independent Test**: Run the graph property suite and verify parity for highest-priority selection, tie handling, empty plans, and post-fire behavior.

**Acceptance Scenarios**:

1. **Given** a `GraphPetriNet` with prioritized transitions, **When** the shared firing plan is derived, **Then** the selected transition matches the current sort-based selection.
2. **Given** a `GraphPetriNet` with no enabled transitions, **When** the shared firing plan is derived, **Then** the plan is empty and firing leaves the marking unchanged.

---

### User Story 5 - Reduced CPU in Selection Path (Priority: P2)

Under benchmark conditions with large nets (many transitions and places), the aligned firing-plan and max-scan selection path must demonstrate measurably lower CPU usage per call compared to the sort-based approach.

**Why this priority**: This is the driving motivation for the change but it remains subordinate to correctness and model alignment.

**Independent Test**: A microbenchmark targeting `GetNextTransitionToFire` on both graph and matrix paths with nets of varying sizes, comparing before/after CPU time.

**Acceptance Scenarios**:

1. **Given** a benchmark harness with a large Petri net (100+ transitions), **When** `GetNextTransitionToFire` is called repeatedly, **Then** CPU per call is at least 20% lower than the LINQ-based baseline.
2. **Given** a benchmark with allocation profiling, **When** `GetNextTransitionToFire` is called in steady state, **Then** zero additional heap allocations occur per call.
3. **Given** the feature branch is ready for validation, **When** benchmark evidence is reviewed, **Then** committed microbenchmarks exist for both `GraphPetriNet` and `MatrixPetriNet` selection paths and can be run to reproduce the reported before/after results.

---

### Edge Cases

- What happens when all transitions have the same priority? The lowest-index enabled transition should be returned.
- What happens when only one transition is enabled? The firing plan contains only that transition.
- What happens when the transition priority map is sparse? Transitions without an explicit priority entry default to priority 0.
- What happens when the net has transitions but none are enabled? The firing plan is empty, no transition handlers are invoked, and firing has no effect.
- What happens when graph and matrix models disagree on whether multiple transitions should fire in the same step? The shared firing plan abstraction defines the canonical step semantics and both models must conform to it.
- What happens when multiple transitions are included in one firing plan? The dispatcher must action them using the canonical plan order defined by the shared firing plan semantics.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Both `GraphPetriNet` and `MatrixPetriNet` MUST derive execution from the same firing plan abstraction so that equivalent nets can be checked for step-level equivalence.
- **FR-002**: The shared firing plan abstraction MUST define canonical behavior for empty plans, single-transition plans, and multi-transition plans where the model semantics allow them.
- **FR-003**: Both `GraphPetriNet` and `MatrixPetriNet` MUST hand derived firing plans to the same dispatcher abstraction to action the plan through the handlers attached to the selected transitions.
- **FR-004**: The shared dispatcher MUST define the observable behavior for empty-plan dispatch, transition-handler invocation, and the logical action order used when a firing plan contains multiple transitions.
- **FR-005**: The max-scan selection MUST return the same transition as the current LINQ `orderby ... descending` + `FirstOrDefault`/`First` implementation for all possible markings and priority configurations when selecting a single transition within the firing plan.
- **FR-006**: When no transitions are enabled, the firing plan MUST be empty, the dispatcher MUST invoke no transition handlers, and `GetNextTransitionToFire` MUST continue to return null in both models.
- **FR-007**: When multiple enabled transitions share the highest priority, the one with the lowest transition index MUST be selected.
- **FR-008**: The firing-plan, dispatcher, and max-scan logic MUST be implemented in both `GraphPetriNet` and `MatrixPetriNet` classes through shared semantics.
- **FR-009**: The public method signature of `GetNextTransitionToFire(Marking m)` MUST remain unchanged in both classes.
- **FR-010**: The `CreateFiringPlan` behavior exposed by both models MUST continue to function correctly for equivalent graph and matrix nets.
- **FR-011**: The `Fire` method in both models MUST consume the shared firing plan semantics and shared dispatcher behavior and produce equivalent results for equivalent nets and markings.
- **FR-012**: The feature MUST include committed, runnable microbenchmarks that measure `GetNextTransitionToFire` on both `GraphPetriNet` and `MatrixPetriNet` using representative large-net inputs.

### Key Entities

- **Firing Plan**: A canonical representation of the transitions selected to fire for a single step under a given marking.
- **Firing Plan Dispatcher**: A shared dispatcher that consumes a firing plan and actions it by invoking the handlers attached to the selected transitions according to canonical dispatch semantics.
- **Transition**: Identified by integer index, has an associated priority (default 0).
- **Transition Handler**: The action-bearing behavior associated with a transition that is invoked when the dispatcher actions that transition as part of a firing plan.
- **Marking**: A vector of token counts per place that determines which transitions are enabled.
- **Priority**: An integer value associated with each transition; higher values mean higher priority. Stored in `TransitionPriorities` dictionary.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All existing unit and property-based tests pass without modification after the change.
- **SC-002**: Transition selection completes at least 20% faster per call in benchmarked large nets (100+ transitions) compared to the LINQ-based baseline.
- **SC-003**: Zero additional heap allocations per `GetNextTransitionToFire` call in steady-state operation.
- **SC-004**: Deterministic tie-breaking behavior is preserved — identical transition is selected for identical inputs before and after the change.
- **SC-005**: Benchmark results are reproducible from committed benchmark code covering both graph and matrix selection paths.
- **SC-006**: Equivalent graph and matrix nets derive equivalent firing plans and equivalent post-fire markings for the same marking inputs.
- **SC-007**: Equivalent graph and matrix nets dispatch equivalent firing plans through the same logical transition-handler action order and invoke the same transition handlers for the same marking inputs.

## Assumptions

- The current LINQ `orderby ... descending` + `FirstOrDefault`/`First` behavior on equal-priority transitions selects the first one encountered in enumeration order, which corresponds to the lowest transition index from the enabled-transition traversal. The max-scan replacement preserves this by preferring lower indices on tie.
- The `GetTransitionPriority` method's default-to-zero behavior for missing priority entries is unchanged and relied upon.
- The shared firing plan abstraction is allowed to restructure how selection results are represented, but it must preserve current observable selection semantics unless canonical graph/matrix alignment requires otherwise.
- The shared dispatcher abstraction is allowed to centralize transition-handler actioning previously embedded in individual models, but it must preserve the externally observable firing behavior for equivalent graph and matrix nets.
- Benchmark infrastructure needed to validate the change is part of this feature's deliverable scope for both graph and matrix paths.
- The shared firing plan abstraction and dispatcher become the canonical source of step semantics and transition actioning for subsequent performance enhancements.
