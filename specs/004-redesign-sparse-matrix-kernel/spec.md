# Feature Specification: Sparse Matrix Fire Kernel Redesign

**Feature Branch**: `005-redesign-sparse-matrix-kernel`  
**Created**: 2026-04-14  
**Status**: Draft  
**Input**: User description: "Sparse matrix fire kernel redesign"

## User Scenarios & Testing *(mandatory)*

Each story is framed so correctness can be validated with property-based tests over families of generated Petri nets and markings.

### User Story 1 - Preserve Firing Correctness Parity (Priority: P1)

As a library consumer, I need matrix-based firing to produce the same behavioral outcomes as the existing baseline so that performance improvements do not change system correctness.

**Why this priority**: Correctness parity is the non-negotiable gate; without it, performance gains are not acceptable.

**Independent Test**: Can be fully tested by running differential property-based tests that execute baseline and redesigned paths on identical generated models and comparing post-fire marking and observable behavior.

**Acceptance Scenarios**:

1. **Given** an identical net definition, marking, and selected transitions, **When** both baseline and redesigned matrix fire paths execute, **Then** both produce the same post-fire marking.
2. **Given** transitions with side effects, **When** both fire paths execute the same selected transitions, **Then** side effects are observed in the same invocation order.
3. **Given** identical inputs repeated across runs, **When** the redesigned path executes, **Then** outcomes remain deterministic across runs.
4. **Given** an active transition set, **When** the redesigned fire path evaluates transition-place connectivity, **Then** it enumerates only known non-zero pre/post incidence paths.
5. **Given** places and transitions with no connectivity for the active transition set, **When** the redesigned fire path runs, **Then** it skips disconnected rows and columns.
6. **Given** matrix paths known to be zero or non-existent, **When** the redesigned fire path evaluates candidate updates, **Then** those paths are not enumerated.
7. **Given** a selected transition set that affects only a subset of places, **When** token updates are applied, **Then** deltas are computed and applied only for affected places.

---

### User Story 2 - Improve Sparse Workload Throughput (Priority: P2)

As a performance-focused operator, I need sparse matrix workloads to complete firing steps significantly faster so that large sparse models scale with lower latency.

**Why this priority**: The redesign target is sparse-model throughput, which directly impacts runtime efficiency for large real-world sparse nets.

**Independent Test**: Can be tested independently with density-sweep benchmarks that measure sparse-profile throughput against baseline under fixed workload families.

**Acceptance Scenarios**:

1. **Given** sparse benchmark profiles and baseline measurements, **When** redesigned matrix firing is benchmarked, **Then** sparse-profile throughput is at least 2.0x baseline.
2. **Given** sparse benchmark profiles, **When** allocation evidence is collected, **Then** allocation rate is non-increasing or any increase is explicitly justified by benchmark evidence.

---

### User Story 3 - Protect Dense and Rollout Safety (Priority: P3)

As a release owner, I need the redesign to avoid unacceptable dense-workload regressions and support controlled rollout/fallback so the change can be introduced safely.

**Why this priority**: Dense regressions and unsafe rollout can negate operational gains and increase production risk.

**Independent Test**: Can be tested independently by running dense-profile benchmarks, existing correctness suites, and rollout toggle checks.

**Acceptance Scenarios**:

1. **Given** dense benchmark profiles and baseline measurements, **When** redesigned matrix firing is benchmarked, **Then** dense-profile throughput regression is no worse than 5%.
2. **Given** rollout configuration with baseline default, **When** fallback mode is selected, **Then** execution uses baseline behavior with no API shape change.

### Edge Cases

- How does firing behave when selected transitions have no non-zero connectivity in the matrix representation?
- How does execution behave when sparse and dense regions coexist in the same model and active transition set?
- What happens when token deltas for affected places net to zero after applying selected transitions?
- What happens when no transitions are enabled but a fire attempt is requested?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST produce the same post-fire marking as the baseline matrix behavior for identical net definitions, markings, and selected transitions.
- **FR-002**: The system MUST preserve transition function invocation side effects and invocation order exactly as baseline behavior.
- **FR-003**: The system MUST preserve enablement and conflict behavior parity with baseline behavior for identical inputs.
- **FR-004**: The redesigned fire path MUST evaluate and apply changes using only known non-zero connectivity for the active transition set, avoiding guaranteed-empty matrix paths.
- **FR-005**: The system MUST apply token deltas only to places affected by selected transitions.
- **FR-006**: The change MUST remain internal to execution logic and MUST NOT alter public API signatures or externally visible model shape.
- **FR-007**: The system MUST provide a rollout control that defaults to baseline behavior and allows immediate fallback to baseline behavior if regressions are detected.
- **FR-008**: The system MUST define contract-sensitive behavior for inputs, outputs, invariants, and failure modes using idiomatic C# expectations rather than deprecated Code Contracts tooling.

### Key Entities *(include if feature involves data)*

- **Fire Input State**: The combination of net definition, current marking, and selected transitions used as input for a fire step.
- **Connectivity Metadata**: The known non-zero incidence relationships that determine which transition-place paths are valid to evaluate.
- **Affected Place Set**: The subset of places whose token counts can change for a given selected transition set.
- **Execution Mode Toggle**: A runtime selection that determines whether baseline or redesigned matrix fire behavior is used.
- **Parity Oracle Result**: The comparable behavioral outputs (marking, side-effect order, enablement/conflict outcomes, determinism observations) produced by baseline and redesigned executions.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For randomized sparse and dense model families, 100% of parity checks between baseline and redesigned matrix firing pass for post-fire marking, side-effect order, and enablement/conflict outcomes.
- **SC-002**: Sparse-profile throughput is at least 2.0x baseline in the benchmark matrix defined for this feature.
- **SC-003**: Dense-profile throughput regression is no worse than 5% relative to baseline in the same benchmark matrix.
- **SC-004**: Determinism checks across repeated identical runs show 100% identical outcomes.
- **SC-005**: Existing correctness suites complete with zero behavioral regressions attributable to this feature.

When performance is evaluated, correctness-preserving parity is validated first, then throughput and allocation outcomes are assessed using the defined benchmark and diagnostic workflow.

## Assumptions

- Baseline matrix execution behavior is the source of truth for parity comparisons during this feature cycle.
- Benchmark scenarios for sparse and dense profiles are reproducible and represent expected production-relevant model families.
- Rollout control is available through existing configuration patterns without changing external API shape.
- Existing correctness and property-based test suites remain authoritative for regression detection.
- This feature excludes SIMD/intrinsics acceleration and broad pooling redesign work unless strictly required for this kernel scope.
