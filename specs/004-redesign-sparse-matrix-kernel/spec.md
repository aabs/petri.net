# Feature Specification: Sparse Matrix Fire Kernel Redesign

**Feature Branch**: `005-redesign-sparse-matrix-kernel`  
**Created**: 2026-04-14  
**Status**: Draft  
**Input**: User description: "Sparse matrix fire kernel redesign"

## Clarifications

### Session 2026-04-14

- Q: Which transition set defines state-equation computation in this feature? -> A: Use the same transition-set semantics as baseline current implementation.
- Q: How should benchmark matrix scale be fixed for acceptance thresholds? -> A: Define only density bands, no fixed tuple sizes.
- Q: How strict should state-equation parity be between baseline and redesigned execution? -> A: Exact equality of full result values.

## Capability Scenarios & Testing *(mandatory)*

Each scenario defines an independently testable technical capability slice validated with property-based tests over generated sparse and dense model families.

All hard constraints from the input are represented directly in acceptance scenarios.

### Capability Slice 1 - Sparse Connectivity-Constrained Execution Traversal (Priority: P1)

**Capability**: Redesign matrix execution traversal so fire and state-equation computation operate only on known non-zero connectivity, without scanning full place x transition space.

**Why this priority**: This is the core technical change requested and the direct source of expected sparse-workload gains.

**Independent Test**: Differential property suite compares baseline and redesigned kernels over generated sparse and dense model families while instrumenting enumerated paths to ensure only non-zero connectivity is traversed for fire and state-equation operations.

**Acceptance Scenarios**:

1. **Given** an active transition set, **When** redesigned fire or state-equation logic evaluates transition-place connectivity, **Then** it enumerates only known non-zero pre/post incidence paths.
2. **Given** places and transitions with no connectivity for the active transition set, **When** redesigned execution logic runs, **Then** it skips disconnected rows and columns.
3. **Given** matrix paths known to be zero or non-existent, **When** candidate update paths are evaluated, **Then** those paths are not enumerated.
4. **Given** a selected transition set that affects only a subset of places, **When** token updates are applied, **Then** deltas are computed and applied only for affected places.

---

### Capability Slice 2 - Behavioral Parity and Determinism Preservation (Priority: P2)

**Capability**: Preserve baseline observable behavior for fire and state-equation computation while changing internal execution representation.

**Why this priority**: Correctness and deterministic behavior are mandatory before performance gains can be accepted.

**Independent Test**: Property-based parity oracle executes baseline and redesigned kernels on identical inputs and compares post-fire marking, state-equation outputs, side-effect order, enablement/conflict outcomes, and repeated-run determinism.

**Acceptance Scenarios**:

1. **Given** identical net definition, marking, and selected transitions, **When** baseline and redesigned kernels execute, **Then** post-fire markings are identical.
2. **Given** identical net definition and marking, **When** baseline and redesigned kernels compute state-equation outputs using baseline transition-set semantics, **Then** the full state-equation result values are exactly equal.
3. **Given** transitions with side effects, **When** both kernels execute the same selected transitions, **Then** side effects occur in the same invocation order.
4. **Given** identical inputs repeated across runs, **When** redesigned kernel execution is repeated, **Then** outcomes remain deterministic.
5. **Given** identical inputs for enablement and conflict evaluation, **When** baseline and redesigned paths are evaluated, **Then** enablement and conflict outcomes are identical.

---

### Capability Slice 3 - Performance Guardrails Across Fire and State Equation (Priority: P3)

**Capability**: Meet sparse throughput targets for fire and state-equation workloads while protecting dense behavior from unacceptable regression.

**Why this priority**: This ensures the migration delivers measurable value on sparse workloads without harming dense workloads.

**Independent Test**: Density-sweep BenchmarkDotNet matrix with MemoryDiagnoser plus existing correctness suite, where acceptance gates are defined by low/medium/high density bands rather than fixed place-transition tuple sizes.

**Acceptance Scenarios**:

1. **Given** sparse benchmark profiles and baseline measurements for fire workload, **When** redesigned kernel benchmarks run, **Then** sparse fire throughput is at least 2.0x baseline.
2. **Given** sparse benchmark profiles and baseline measurements for state-equation workload, **When** redesigned kernel benchmarks run, **Then** sparse state-equation throughput is at least 2.0x baseline.
3. **Given** dense benchmark profiles and baseline measurements for fire and state-equation workloads, **When** redesigned kernel benchmarks run, **Then** dense-profile throughput regression is no worse than 5% for both workloads.
4. **Given** sparse benchmark profiles, **When** allocation evidence is captured, **Then** allocation rate is non-increasing or any increase is explicitly justified by benchmark evidence.

### Edge Cases

- Selected transitions have no non-zero connectivity in the matrix representation.
- Sparse and dense subregions coexist within one model and one active transition set.
- Token deltas for affected places net to zero.
- No transitions are enabled and a fire attempt is requested.
- Active transitions touch all places, approaching dense behavior.
- State-equation evaluation is requested for models with no non-zero incidence entries.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The redesigned matrix execution path MUST enumerate only known non-zero pre/post incidence paths for fire and state-equation operations.
- **FR-002**: The redesigned matrix execution path MUST skip rows and columns with no connectivity for active transition and place computations.
- **FR-003**: The redesigned matrix execution path MUST NOT enumerate paths known to be zero or non-existent.
- **FR-004**: The redesigned matrix fire path MUST apply token deltas only to places affected by selected transitions.
- **FR-005**: The system MUST produce the same post-fire marking as baseline for identical net definitions, markings, and selected transitions.
- **FR-006**: The system MUST produce state-equation outputs equivalent to baseline for identical net definitions and markings.
- **FR-013**: The redesigned state-equation computation MUST use the same transition-set semantics as the baseline implementation.
- **FR-007**: The system MUST preserve transition function invocation side effects and invocation order exactly as baseline.
- **FR-008**: The system MUST preserve enablement and conflict behavior parity with baseline for identical inputs.
- **FR-009**: Execution MUST remain deterministic for identical inputs.
- **FR-010**: The internal representation MAY use any sparse implementation approach, provided FR-001 through FR-009 and FR-011 are satisfied.
- **FR-011**: The change MUST be internal to execution logic and MUST NOT alter public API signatures or externally visible model shape.
- **FR-012**: The system MUST define contract-sensitive behavior for inputs, outputs, invariants, and failure modes using idiomatic C# expectations rather than deprecated Code Contracts tooling.
- **FR-014**: Benchmark acceptance gates MUST be defined by density bands (low/medium/high) and need not require fixed place-transition tuple sizes.
- **FR-015**: State-equation parity between baseline and redesigned execution MUST be verified using exact equality of full result values.

### Key Entities *(include if feature involves data)*

- **Fire Input State**: Net definition, current marking, and selected transitions for one fire step.
- **Connectivity Metadata**: Known non-zero incidence relationships used to determine valid traversal and update paths.
- **Affected Place Set**: Subset of places whose token values can change for a selected transition set.
- **State Equation Result**: Computed state-equation output for a given net definition and marking.
- **Parity Oracle Result**: Comparable outputs across kernels, including marking, side-effect order, enablement/conflict outcomes, and determinism.

### Scenario to Requirement Traceability *(mandatory)*

- **Capability Slice 1**: FR-001, FR-002, FR-003, FR-004
- **Capability Slice 2**: FR-005, FR-006, FR-007, FR-008, FR-009, FR-013, FR-015
- **Capability Slice 3**: FR-010, FR-011, FR-012, FR-014

For each FR, at least one acceptance scenario proves coverage:
- **FR-001 proven by**: Slice 1, Scenario 1
- **FR-002 proven by**: Slice 1, Scenario 2
- **FR-003 proven by**: Slice 1, Scenario 3
- **FR-004 proven by**: Slice 1, Scenario 4
- **FR-005 proven by**: Slice 2, Scenario 1
- **FR-006 proven by**: Slice 2, Scenario 2
- **FR-013 proven by**: Slice 2, Scenario 2
- **FR-007 proven by**: Slice 2, Scenario 3
- **FR-008 proven by**: Slice 2, Scenario 5
- **FR-009 proven by**: Slice 2, Scenario 4
- **FR-010 proven by**: Slice 1, Scenarios 1-3 and Slice 3, Scenarios 1-4
- **FR-011 proven by**: Slice 2, Scenarios 1-2 and Slice 3, Scenario 3
- **FR-012 proven by**: Slice 2, Scenarios 1-2 and Slice 3, Scenario 4
- **FR-014 proven by**: Slice 3, Scenarios 1-3
- **FR-015 proven by**: Slice 2, Scenario 2

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For randomized sparse and dense model families, 100% parity checks pass for post-fire marking, exact state-equation result-value equality, side-effect order, and enablement/conflict outcomes.
- **SC-002**: Sparse-profile fire throughput is at least 2.0x baseline in the benchmark matrix defined for this feature.
- **SC-003**: Sparse-profile state-equation throughput is at least 2.0x baseline in the benchmark matrix defined for this feature.
- **SC-004**: Dense-profile throughput regression is no worse than 5% relative to baseline for both fire and state-equation workloads.
- **SC-005**: Determinism checks across repeated identical runs show 100% identical outcomes.
- **SC-006**: Existing correctness suites pass with zero behavioral regressions attributable to this feature.
- **SC-007**: Sparse-workload allocation rate is non-increasing relative to baseline, or increases are benchmark-justified and documented.

Correctness-preserving parity is validated before performance and allocation targets are assessed.

## Assumptions

- Baseline matrix execution behavior is the parity source of truth for this cycle.
- Sparse and dense benchmark profiles are reproducible and representative of targeted workloads.
- Benchmark acceptance uses density-band profiles rather than fixed place-transition tuple sizes.
- Runtime fallback to baseline is not required after migration, provided parity and performance criteria are met.
- Existing property-based and correctness suites remain authoritative regression gates.
- SIMD and intrinsics optimization, and broad pooling redesign, remain out of scope for this feature.
