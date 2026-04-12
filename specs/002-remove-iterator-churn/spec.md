# Feature Specification: Hot-Path Allocation Removal

**Feature Branch**: `002-remove-iterator-churn`  
**Created**: 2026-04-12  
**Status**: Draft  
**Input**: User description: "Remove hot-path materialization and iterator churn"

## User Scenarios & Testing *(mandatory)*

The primary stakeholder scenario focuses on reducing steady-state allocation pressure in proven hotspots while cross-cutting semantic parity remains enforced by the requirements and success criteria.

### User Story 1 - Reduce runtime allocation pressure (Priority: P1)

As an operator running large state-driven models, I need the engine to eliminate unnecessary transient allocations and repeated iteration in known steady-state hotspots so sustained workloads show lower latency and less GC disruption.

**Why this priority**: Once correctness is preserved, the main user value is lower memory churn and better tail-latency in workloads where transition execution is repeated at high frequency, especially in the enablement, firing, and marking paths already identified by the remediation analysis.

**Independent Test**: Can be fully tested by benchmark and replay runs that compare bytes per operation and tail-latency against the current baseline for the same workload.

**Acceptance Scenarios**:

1. **Given** a representative firing benchmark, **When** the optimized hot path is measured, **Then** bytes per operation are reduced by at least 30% relative to the baseline.
2. **Given** a representative load replay, **When** the optimized hot path is measured under the same workload, **Then** p99 latency improves by at least 10% relative to the baseline.
3. **Given** a hotspot that currently materializes temporary collections or performs repeated enumerable passes, **When** the remediation for that hotspot is applied, **Then** the same observable outcome is produced with fewer temporary allocations and fewer passes over the same data.

### Edge Cases

- How does the optimized path behave when no transitions are enabled and the current engine returns an empty or no-op result?
- How does the optimized path behave when repeated firing steps invoke transition functions with observable side effects that depend on invocation order?
- What happens when benchmarked workloads are too small to show meaningful allocation improvement, even though correctness is preserved?
- How does the optimized path preserve behavior when current enablement or conflict checks traverse the same transition candidates more than once?
- How does the optimized path preserve behavior when current firing or marking flows clone or materialize intermediate state before applying updates?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST preserve exact transition enablement results for the same model state before and after each optimization change.
- **FR-002**: The system MUST preserve exact firing outcomes for the same starting marking and selected transitions before and after each optimization change.
- **FR-003**: The system MUST preserve transition function invocation order and invocation count within each firing step.
- **FR-004**: The system MUST remove avoidable transient materialization in proven hot execution paths, specifically targeting temporary lists, arrays, and equivalent intermediate collections created only to support immediate enablement, conflict, firing, or marking work.
- **FR-005**: The system MUST reduce repeated traversal of the same transition, arc, or marking data in proven hot execution paths by preferring single-pass evaluation, early exit, or equivalent bounded iteration where observable behavior is unchanged.
- **FR-006**: The system MUST replace iterator-heavy hot-path sequence processing only where the remediation evidence shows measurable allocation or traversal cost, rather than applying broad style rewrites across non-hot code.
- **FR-007**: The primary R2 implementation scope MUST cover the hotspot locations already identified in the remediation analysis in [src/core/PetriNetBase.cs](src/core/PetriNetBase.cs#L16), [src/core/PetriNetBase.cs](src/core/PetriNetBase.cs#L22), [src/core/PetriNetBase.cs](src/core/PetriNetBase.cs#L28), [src/core/GraphPetriNet.cs](src/core/GraphPetriNet.cs#L221), [src/core/GraphPetriNet.cs](src/core/GraphPetriNet.cs#L306), [src/core/GraphPetriNet.cs](src/core/GraphPetriNet.cs#L333), [src/core/MatrixPetriNet.cs](src/core/MatrixPetriNet.cs#L252), [src/core/MatrixPetriNet.cs](src/core/MatrixPetriNet.cs#L282), [src/core/MatrixPetriNet.cs](src/core/MatrixPetriNet.cs#L290), and [src/core/MatrixPetriNet.cs](src/core/MatrixPetriNet.cs#L292). The `Marking` copy sites in [src/core/Marking.cs](src/core/Marking.cs#L33) and [src/core/Marking.cs](src/core/Marking.cs#L64) MUST be measured and reported as supporting allocation context for this feature, but they do not require direct ownership or mutability changes in R2.
- **FR-008**: The system MUST leave token update semantics unchanged.
- **FR-009**: The feature MUST remain limited to hot-path materialization and iterator-churn removal within the identified R2 hotspots and exclude reverse lookup maps, PNML traversal redesign, sparse matrix kernel redesign, and in-place marking semantics changes.

### Key Entities *(include if feature involves data)*

- **Hot Execution Path**: A repeatedly executed engine path whose allocation rate or iterator churn has a measurable effect on throughput or latency.
- **Firing Step**: A single unit of execution that evaluates enablement, selects or receives a transition to fire, and applies the resulting marking update.
- **Transition Invocation Sequence**: The ordered set of transition-function calls that occur during a firing step and whose order must remain unchanged.
- **Materialization Site**: A hotspot location where temporary lists, arrays, cloned state, or equivalent transient objects are created only to serve immediately following execution logic.
- **Repeated Traversal Site**: A hotspot location where the same transition, arc, or marking data is enumerated multiple times within a single decision or firing step.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In the representative firing benchmark suite, optimized method groups reduce bytes per operation by at least 30% while preserving baseline-correct behavior.
- **SC-002**: When a representative load replay harness already exists for the branch or local environment, optimized method groups improve p99 latency by at least 10% while preserving the same externally observable firing outcomes; otherwise, this feature records the replay gap without blocking R2 acceptance.
- **SC-003**: All existing and newly added parity tests covering enablement results, firing outcomes, invocation order, and no-transition cases pass with zero newly introduced failures.
- **SC-004**: Allocation profiling for each changed hotspot shows that the targeted materialization or iterator-churn source at the affected location is reduced or eliminated relative to the baseline run for the same workload.

When performance is a goal, success criteria MUST state the correctness-preserving boundary first and then define the measurable performance target and validation method.

## Assumptions

- Existing benchmark and load replay scenarios are representative enough to detect meaningful allocation and tail-latency changes in the targeted hot paths.
- Hot-path candidates are selected based on measured evidence rather than broad style rewrites across non-critical code.
- Public APIs and currently observable engine behavior remain in scope to preserve throughout this work.
- The current remediation evidence for this feature is centered on base enable/conflict logic, graph execution, matrix execution, and marking materialization rather than on builder lookups, PNML traversal, or sparse-kernel redesign.
- Guidance note: preserving exact execution semantics is treated here as a cross-cutting engineering constraint rather than a standalone user scenario; if that rule should apply to performance work across the repository, it belongs in constitution-level guidance.
- Planning note: sequencing changes into small rollout slices and capturing per-slice benchmark evidence belong in `/speckit.plan` and related tasks, not as standalone user stories in this specification.
