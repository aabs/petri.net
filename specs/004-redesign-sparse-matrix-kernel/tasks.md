# Tasks: Sparse Matrix Fire Kernel Redesign

**Input**: Design documents from /specs/004-redesign-sparse-matrix-kernel/
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Property-based tests are required. Every capability slice includes FsCheck/FsCheck.Xunit properties that must be written first, observed failing, and only then followed by implementation tasks.

**Organization**: Tasks are grouped by capability slice (mapped to US1/US2/US3 labels for traceability) so each slice remains independently implementable and testable.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish deterministic scenario generation and benchmark scaffolding.

- [X] T001 Create sparse execution property scenario builders in test/core.tests/SparseMatrixKernelPropertyData.cs
- [X] T002 [P] Create state-equation benchmark scenario builders in perf/core.benchmarks/StateEquationBenchmarkScenarios.cs
- [X] T003 [P] Create sparse/dense density-band benchmark scenario builders in perf/core.benchmarks/SparseMatrixBenchmarkScenarios.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared test and benchmark plumbing required before capability implementation.

**CRITICAL**: No capability-slice implementation should begin until this phase is complete.

- [X] T004 Create shared sparse-kernel benchmark harness with MemoryDiagnoser in perf/core.benchmarks/SparseMatrixKernelBenchmarks.cs
- [X] T005 [P] Register sparse-kernel and state-equation benchmarks in perf/core.benchmarks/Program.cs
- [ ] T006 [P] Register FsCheck arbitraries for sparse/dense model families in test/core.tests/globals.cs
- [X] T007 Add baseline-vs-redesigned differential oracle helpers in test/core.tests/SparseMatrixKernelParityOracle.cs

**Checkpoint**: Shared property and benchmark infrastructure is ready.

---

## Phase 3: Capability Slice 1 - Sparse Connectivity-Constrained Execution Traversal (Priority: P1) 🎯 MVP

**Goal**: Ensure fire and state-equation execution enumerate only known non-zero connectivity and avoid full-grid scans.

**Independent Test**: Differential properties verify non-zero-only traversal, disconnected-row/column skipping, and affected-place-only updates.

### Tests for Capability Slice 1 ⚠️

- [X] T008 [P] [US1] Add FsCheck.Xunit property for non-zero-only traversal invariant in test/core.tests/MatrixPetriNetSparseTraversalProperties.cs
- [X] T009 [P] [US1] Add FsCheck.Xunit property for disconnected rows/columns skip invariant in test/core.tests/MatrixPetriNetSparseTraversalProperties.cs
- [X] T010 [P] [US1] Add FsCheck.Xunit property for no enumeration of zero/non-existent paths in test/core.tests/MatrixPetriNetSparseTraversalProperties.cs
- [X] T011 [P] [US1] Add FsCheck.Xunit property for affected-place-only token updates in test/core.tests/MatrixPetriNetSparseTraversalProperties.cs

### Implementation for Capability Slice 1

- [X] T012 [US1] Introduce sparse connectivity index structures in src/core/MatrixPetriNet.cs
- [X] T013 [US1] Implement sparse fire traversal over known non-zero connectivity in src/core/MatrixPetriNet.cs
- [X] T014 [US1] Implement affected-place set aggregation and bounded token-delta application in src/core/MatrixPetriNet.cs
- [X] T015 [US1] Remove full place x transition scan dependency in fire execution path in src/core/MatrixPetriNet.cs
- [X] T016 [US1] Refactor sparse traversal helpers for readability while preserving green properties in src/core/MatrixPetriNet.cs

**Checkpoint**: Capability Slice 1 is independently functional and testable.

---

## Phase 4: Capability Slice 2 - Behavioral Parity and Determinism Preservation (Priority: P2)

**Goal**: Preserve baseline behavior for fire and state-equation outputs, side-effect order, enablement/conflict, and determinism.

**Independent Test**: Differential parity properties compare baseline and redesigned outputs with exact state-equation value equality.

### Tests for Capability Slice 2 ⚠️

- [X] T017 [P] [US2] Add FsCheck.Xunit property for post-fire marking parity in test/core.tests/MatrixPetriNetParityProperties.cs
- [X] T018 [P] [US2] Add FsCheck.Xunit property for exact state-equation result-value equality in test/core.tests/MatrixPetriNetStateEquationProperties.cs
- [X] T019 [P] [US2] Add FsCheck.Xunit property for state-equation baseline transition-set semantics in test/core.tests/MatrixPetriNetStateEquationProperties.cs
- [X] T020 [P] [US2] Add FsCheck.Xunit property for side-effect invocation order parity in test/core.tests/MatrixPetriNetParityProperties.cs
- [X] T021 [P] [US2] Add FsCheck.Xunit property for enablement/conflict outcome parity in test/core.tests/MatrixPetriNetParityProperties.cs
- [X] T022 [P] [US2] Add FsCheck.Xunit property for determinism under repeated identical inputs in test/core.tests/MatrixPetriNetParityProperties.cs

### Implementation for Capability Slice 2

- [ ] T023 [US2] Implement state-equation computation over sparse execution representation in src/core/MatrixPetriNet.cs
- [ ] T024 [US2] Preserve baseline transition-set semantics for state-equation computation in src/core/MatrixPetriNet.cs
- [ ] T025 [US2] Ensure transition side-effect invocation order parity in fire execution in src/core/MatrixPetriNet.cs
- [ ] T026 [US2] Preserve enablement/conflict parity in shared execution flow in src/core/PetriNetBase.cs
- [ ] T027 [US2] Add idiomatic contract guards/assertions for updated execution paths in src/core/MatrixPetriNet.cs
- [ ] T028 [US2] Refactor parity-critical helpers without changing behavior in src/core/MatrixPetriNet.cs

**Checkpoint**: Capability Slice 2 is independently functional and testable.

---

## Phase 5: Capability Slice 3 - Performance Guardrails Across Fire and State Equation (Priority: P3)

**Goal**: Demonstrate sparse throughput gains and dense-regression guardrails using density-band benchmark gates.

**Independent Test**: Benchmarks validate SC-002 through SC-004 and SC-007 using low/medium/high density profiles.

### Tests and Benchmarks for Capability Slice 3 ⚠️

- [X] T029 [P] [US3] Add benchmark scenarios for fire throughput across low/medium/high density bands in perf/core.benchmarks/SparseMatrixBenchmarkScenarios.cs
- [X] T030 [P] [US3] Add benchmark scenarios for state-equation throughput across low/medium/high density bands in perf/core.benchmarks/StateEquationBenchmarkScenarios.cs
- [ ] T031 [P] [US3] Add benchmark assertions/report checks for <=5% dense regression on fire and state-equation workloads in perf/core.benchmarks/SparseMatrixKernelBenchmarks.cs
- [X] T032 [P] [US3] Add allocation-focused benchmark capture for sparse workloads in perf/core.benchmarks/SparseMatrixKernelBenchmarks.cs

### Implementation for Capability Slice 3

- [X] T033 [US3] Optimize sparse execution hot paths based on benchmark evidence in src/core/MatrixPetriNet.cs
- [ ] T034 [US3] Optimize state-equation sparse path for density-band gains in src/core/MatrixPetriNet.cs
- [X] T035 [US3] Update benchmark registration and filters for sparse fire/state-equation suites in perf/core.benchmarks/Program.cs
- [X] T036 [US3] Record benchmark gate expectations and command workflow in specs/004-redesign-sparse-matrix-kernel/quickstart.md

**Checkpoint**: Capability Slice 3 is independently functional and benchmark-verified.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final consistency, evidence capture, and closure tasks across slices.

- [X] T037 [P] Verify external MatrixPetriNet API signature compatibility against baseline in src/core/MatrixPetriNet.cs
- [X] T038 [P] Update contract traceability for FR-014 and FR-015 in specs/004-redesign-sparse-matrix-kernel/contracts/matrix-fire-kernel-contract.md
- [X] T039 Run full regression/property suite and record outcomes in specs/004-redesign-sparse-matrix-kernel/quickstart.md
- [ ] T040 Run full benchmark suite and record SC-002 through SC-007 evidence in specs/004-redesign-sparse-matrix-kernel/quickstart.md
- [X] T041 Update implementation status and completion checklist in specs/004-redesign-sparse-matrix-kernel/plan.md

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1): no dependencies.
- Foundational (Phase 2): depends on Setup; blocks capability slices.
- Capability slices (Phases 3-5): depend on Foundational completion.
- Polish (Phase 6): depends on all capability slices.

### Capability Slice Dependencies

- US1 (P1): starts after Phase 2; no dependency on other slices.
- US2 (P2): starts after US1 traversal primitives are in place.
- US3 (P3): starts after US1 and US2 parity behavior is stable.

### Within Each Capability Slice

- Write property tasks first and confirm they fail for intended reasons.
- Implement minimal production changes to satisfy properties.
- Refactor only after properties are green.
- Keep contracts and quickstart aligned with behavior and benchmark evidence.

---

## Parallel Opportunities

- T002 and T003 can run in parallel in Setup.
- T005, T006, and T007 can run in parallel after T004.
- In US1, T008 through T011 can run in parallel.
- In US2, T017 through T022 can run in parallel.
- In US3, T029 through T032 can run in parallel.
- In Polish, T037, T038, and T041 can run in parallel.

### Parallel Example: Capability Slice 2

```bash
Task: "Add exact state-equation equality property in test/core.tests/MatrixPetriNetStateEquationProperties.cs"
Task: "Add side-effect invocation order parity property in test/core.tests/MatrixPetriNetParityProperties.cs"
Task: "Add enablement/conflict parity property in test/core.tests/MatrixPetriNetParityProperties.cs"
Task: "Add determinism property in test/core.tests/MatrixPetriNetParityProperties.cs"
```

---

## Implementation Strategy

### MVP First (Capability Slice 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete US1 test-first flow in Phase 3.
3. Validate US1 independently with property suite.
4. Pause for review before parity/performance slices.

### Incremental Delivery

1. Deliver US1 (sparse traversal correctness).
2. Deliver US2 (parity and determinism, including exact state-equation equality).
3. Deliver US3 (performance guardrails and benchmark gates).
4. Complete polish and evidence capture.

### Parallel Team Strategy

1. One developer handles execution-kernel implementation in src/core/MatrixPetriNet.cs.
2. One developer handles property suites in test/core.tests/.
3. One developer handles benchmark suites in perf/core.benchmarks/.

---

## Notes

- [P] tasks indicate different files with no direct dependencies.
- [USx] labels map to capability slices for traceability.
- FR-014 is enforced by density-band benchmark tasks (T029-T031, T036).
- FR-015 is enforced by exact state-equation parity tasks (T018, T023, T024).
- Property-first order is mandatory for each capability slice.
