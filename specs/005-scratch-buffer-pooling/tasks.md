# Tasks: Scratch Buffer Pooling

**Input**: Design documents from /specs/005-scratch-buffer-pooling/
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Property-based tests are required. Every capability slice includes FsCheck/FsCheck.Xunit properties that must be written first, observed failing, and only then followed by implementation tasks.

**Organization**: Tasks are grouped by capability slice (mapped to US1/US2/US3 labels for traceability) so each slice remains independently implementable and testable.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish deterministic workload generators, test fixtures, and benchmark scaffolding for pooling scenarios.

- [ ] T001 Create pooling property scenario builders in test/core.tests/ScratchBufferPoolingPropertyData.cs
- [ ] T002 [P] Create contention and retry scenario builders in test/core.tests/ScratchBufferContentionPropertyData.cs
- [ ] T003 [P] Create pooling benchmark scenario builders in perf/core.benchmarks/ScratchBufferPoolingBenchmarkScenarios.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared primitives and hooks that MUST exist before capability implementation.

**CRITICAL**: No capability-slice implementation should begin until this phase is complete.

- [ ] T004 Create lease-tracking test oracle helpers in test/core.tests/ScratchBufferPoolingOracle.cs
- [ ] T005 [P] Register pooling benchmark suite in perf/core.benchmarks/Program.cs
- [ ] T006 [P] Register FsCheck arbitraries for pooling model families in test/core.tests/globals.cs
- [ ] T007 Introduce internal scratch-lease and pool configuration types in src/core/ScratchBufferPooling.cs
- [ ] T008 Add internal acquisition-exhaustion exception type in src/core/ScratchBufferPooling.cs

**Checkpoint**: Shared property, benchmark, and internal pool primitives are ready.

---

## Phase 3: Capability Slice 1 - Pooled Steady-State Scratch Reuse (Priority: P1) 🎯 MVP

**Goal**: Reuse planning and firing scratch buffers in steady-state cycles while preserving baseline planning/fire outcomes.

**Independent Test**: Differential properties compare pooled and baseline planning/fire outputs across repeated workloads and verify reuse behavior.

### Tests for Capability Slice 1 ⚠️

- [ ] T009 [P] [US1] Add FsCheck.Xunit property for planning-output parity under reuse in test/core.tests/ScratchBufferPoolingParityProperties.cs
- [ ] T010 [P] [US1] Add FsCheck.Xunit property for firing-output parity under reuse in test/core.tests/ScratchBufferPoolingParityProperties.cs
- [ ] T011 [P] [US1] Add FsCheck.Xunit property for steady-state scratch reuse (no per-cycle fresh-buffer requirement) in test/core.tests/ScratchBufferPoolingReuseProperties.cs
- [ ] T012 [P] [US1] Add FsCheck.Xunit property for empty/zero-workload edge cases in test/core.tests/ScratchBufferPoolingReuseProperties.cs

### Implementation for Capability Slice 1

- [ ] T013 [US1] Implement planning scratch acquisition/release around transition selection in src/core/FiringPlanner.cs
- [ ] T014 [US1] Implement firing-delta scratch acquisition/release in src/core/GraphPetriNet.cs
- [ ] T015 [US1] Implement firing-delta scratch acquisition/release in src/core/MatrixPetriNet.cs
- [ ] T016 [US1] Wire pooled scratch helper into shared execution flow in src/core/PetriNetBase.cs
- [ ] T017 [US1] Add idiomatic guard clauses for pool inputs and lease lifecycle boundaries in src/core/ScratchBufferPooling.cs
- [ ] T018 [US1] Refactor pooling helpers for readability after US1 properties are green in src/core/ScratchBufferPooling.cs

**Checkpoint**: Capability Slice 1 is independently functional and testable.

---

## Phase 4: Capability Slice 2 - Contract-Safe Reuse Under Concurrency (Priority: P2)

**Goal**: Preserve thread safety, deterministic outcomes, and explicit ownership semantics under concurrent and reentrant execution.

**Independent Test**: Properties validate no contamination/leaks/double-return, and deterministic outcomes under repeated identical inputs and concurrent activity.

### Tests for Capability Slice 2 ⚠️

- [ ] T019 [P] [US2] Add FsCheck.Xunit property for no cross-call contamination invariant in test/core.tests/ScratchBufferOwnershipProperties.cs
- [ ] T020 [P] [US2] Add FsCheck.Xunit property for no double-return invariant in test/core.tests/ScratchBufferOwnershipProperties.cs
- [ ] T021 [P] [US2] Add FsCheck.Xunit property for lease release on exception paths in test/core.tests/ScratchBufferOwnershipProperties.cs
- [ ] T022 [P] [US2] Add FsCheck.Xunit property for deterministic outputs under repeated identical concurrent inputs in test/core.tests/ScratchBufferDeterminismProperties.cs
- [ ] T023 [P] [US2] Add FsCheck.Xunit property for reentrant/nested invocation safety in test/core.tests/ScratchBufferDeterminismProperties.cs

### Implementation for Capability Slice 2

- [ ] T024 [US2] Implement thread-local pool ownership model and per-thread isolation in src/core/ScratchBufferPooling.cs
- [ ] T025 [US2] Implement explicit lease-state transitions (acquired/released/faulted) in src/core/ScratchBufferPooling.cs
- [ ] T026 [US2] Enforce exactly-once return semantics and use-after-release guards in src/core/ScratchBufferPooling.cs
- [ ] T027 [US2] Ensure exception-safe release/fault paths in planning and fire operations in src/core/FiringPlanner.cs
- [ ] T028 [US2] Ensure exception-safe release/fault paths in matrix fire flows in src/core/MatrixPetriNet.cs
- [ ] T046 [US2] Ensure exception-safe release/fault paths in graph fire flows in src/core/GraphPetriNet.cs
- [ ] T029 [US2] Refactor concurrency-critical sections while preserving green ownership/determinism properties in src/core/ScratchBufferPooling.cs

**Checkpoint**: Capability Slice 2 is independently functional and testable.

---

## Phase 5: Capability Slice 3 - Scope-Bounded Allocation Reduction (Priority: P3)

**Goal**: Apply pooling only to targeted steady-state hotspots and verify measurable allocation reduction without unacceptable throughput regression.

**Independent Test**: Benchmark and property evidence confirms scope boundaries, performance targets, and bounded retry/exhaustion behavior.

### Tests and Benchmarks for Capability Slice 3 ⚠️

- [ ] T030 [P] [US3] Add FsCheck.Xunit property for fallback-exhaustion failure atomicity in test/core.tests/ScratchBufferFailureModeProperties.cs
- [ ] T031 [P] [US3] Add FsCheck.Xunit property for 5-retry exponential-backoff attempt budget invariant in test/core.tests/ScratchBufferFailureModeProperties.cs
- [ ] T032 [P] [US3] Add benchmark scenarios for steady-state planning allocation rate in perf/core.benchmarks/ScratchBufferPoolingBenchmarkScenarios.cs
- [ ] T033 [P] [US3] Add benchmark scenarios for steady-state firing allocation rate in perf/core.benchmarks/ScratchBufferPoolingBenchmarkScenarios.cs
- [ ] T034 [P] [US3] Add benchmark comparisons for throughput-regression guardrails in perf/core.benchmarks/ScratchBufferPoolingBenchmarks.cs
- [ ] T047 [P] [US3] Add benchmark replay determinism scenarios for repeated identical inputs in perf/core.benchmarks/ScratchBufferPoolingBenchmarks.cs
- [ ] T048 [P] [US3] Add explicit concurrency stress validation scenarios and zero-violation assertions in perf/core.benchmarks/ScratchBufferPoolingBenchmarks.cs

### Implementation for Capability Slice 3

- [ ] T035 [US3] Implement configurable per-thread retained-capacity cap and overflow routing in src/core/ScratchBufferPooling.cs
- [ ] T036 [US3] Implement shared fallback acquisition with exactly 5 retries and exponential backoff in src/core/ScratchBufferPooling.cs
- [ ] T037 [US3] Implement explicit acquisition-exhaustion exception path with no partial mutation in src/core/PetriNetBase.cs
- [ ] T038 [US3] Bound pooling scope to targeted planning/fire hot paths only in src/core/FiringPlanner.cs
- [ ] T039 [US3] Add benchmark harness and MemoryDiagnoser for pooling suite in perf/core.benchmarks/ScratchBufferPoolingBenchmarks.cs
- [ ] T040 [US3] Update quickstart benchmark commands and acceptance thresholds in specs/005-scratch-buffer-pooling/quickstart.md
- [ ] T049 [US3] Validate default per-thread cap value and override bounds behavior in test/core.tests/ScratchBufferFailureModeProperties.cs

**Checkpoint**: Capability Slice 3 is independently functional and benchmark-verified.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final consistency, traceability, and full validation evidence across slices.

- [ ] T041 [P] Verify public API compatibility remains unchanged in src/core/GraphPetriNet.cs and src/core/MatrixPetriNet.cs
- [ ] T042 [P] Update requirement traceability notes in specs/005-scratch-buffer-pooling/plan.md
- [ ] T043 [P] Align behavioral/failure contract details with implemented semantics in specs/005-scratch-buffer-pooling/contracts/scratch-buffer-pooling-contract.md
- [ ] T044 Run full property/regression suite and record outcomes in specs/005-scratch-buffer-pooling/quickstart.md
- [ ] T045 Run full pooling benchmark suite and record SC-002/SC-003/SC-005 evidence in specs/005-scratch-buffer-pooling/quickstart.md
- [ ] T050 Record SC-004 benchmark replay determinism evidence in specs/005-scratch-buffer-pooling/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1): no dependencies.
- Foundational (Phase 2): depends on Setup; blocks all capability slices.
- Capability slices (Phases 3-5): depend on Foundational completion.
- Polish (Phase 6): depends on all capability slices.

### Capability Slice Dependencies

- US1 (P1): starts after Phase 2; no dependency on other slices.
- US2 (P2): starts after US1 introduces pooled execution boundaries.
- US3 (P3): starts after US1 and US2 establish parity and ownership safety.

### Within Each Capability Slice

- Write property tasks first and confirm they fail for intended reasons.
- Implement minimal production changes to satisfy properties.
- Refactor only after properties are green.
- Keep contracts and quickstart aligned with behavior and benchmark evidence.

---

## Parallel Opportunities

- T002 and T003 can run in parallel in Setup.
- T005 and T006 can run in parallel after T004.
- In US1, T009 through T012 can run in parallel.
- In US2, T019 through T023 can run in parallel.
- In US3, T030 through T034 can run in parallel.
- In Polish, T041 through T043 can run in parallel.

### Parallel Example: Capability Slice 2

```bash
Task: "Add no cross-call contamination property in test/core.tests/ScratchBufferOwnershipProperties.cs"
Task: "Add no double-return property in test/core.tests/ScratchBufferOwnershipProperties.cs"
Task: "Add deterministic concurrent-output property in test/core.tests/ScratchBufferDeterminismProperties.cs"
```

---

## Implementation Strategy

### MVP First (Capability Slice 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete US1 test-first flow in Phase 3.
3. Validate US1 independently with property suite.
4. Pause for review before concurrency and performance slices.

### Incremental Delivery

1. Deliver US1 (steady-state pooled reuse parity).
2. Deliver US2 (ownership/thread-safety/determinism safeguards).
3. Deliver US3 (bounded retries, failure atomicity, and performance guardrails).
4. Complete polish and evidence capture.

### Parallel Team Strategy

1. One developer handles core pooling internals in src/core/ScratchBufferPooling.cs.
2. One developer handles property suites in test/core.tests/.
3. One developer handles benchmark scenarios in perf/core.benchmarks/.

---

## Notes

- [P] tasks indicate different files with no direct dependencies.
- [USx] labels map to capability slices for traceability.
- Property-first order is mandatory for every capability slice.
- Retry behavior is intentionally specified as internal implementation detail, not a package-level dependency.
