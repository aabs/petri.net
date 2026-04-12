# Tasks: Max-Scan Transition Selection

**Input**: Design documents from `/specs/001-max-scan-selection/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Property-based tests are required for every user story. Each story begins with FsCheck/FsCheck.Xunit properties that must be written first and observed failing before production code changes begin.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the benchmark project structure and solution wiring required by the feature plan.

- [X] T001 Create benchmark project file in perf/core.benchmarks/core.benchmarks.csproj
- [X] T002 [P] Create benchmark entry point in perf/core.benchmarks/Program.cs
- [X] T003 Add perf/core.benchmarks/core.benchmarks.csproj to petrinets2.slnx

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the shared firing plan abstraction, shared dispatcher, shared generators, and benchmark fixtures that all user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Create FsCheck.Xunit property file for shared firing plan equivalence in test/core.tests/FiringPlanProperties.cs
- [X] T005 Create the shared firing plan abstraction in src/core/FiringPlan.cs
- [X] T006 Create the shared firing planner contract and implementation entry point in src/core/FiringPlanner.cs
- [X] T007 Create the shared firing-plan dispatcher in src/core/FiringPlanDispatcher.cs
- [X] T008 Refactor base execution plumbing to expose shared firing-plan dispatch in src/core/PetriNetBase.cs
- [X] T009 [P] Create shared property generators for transition selection scenarios in test/core.tests/TransitionSelectionPropertyData.cs
- [X] T010 [P] Create reusable benchmark scenario builders in perf/core.benchmarks/TransitionSelectionBenchmarkScenarios.cs

**Checkpoint**: Shared firing plan semantics, dispatch plumbing, property data, and benchmark scaffolding are ready for story work.

---

## Phase 3: User Story 1 - Shared Firing Plan Semantics (Priority: P1) 🎯 MVP

**Goal**: Align graph and matrix execution on one shared firing plan abstraction before optimizing transition selection.

**Independent Test**: Run the new FsCheck.Xunit property suites in test/core.tests/FiringPlanProperties.cs, test/core.tests/FiringPlanDispatcherProperties.cs, and test/core.tests/GraphPetriNetFireProperties.cs and verify that equivalent graph and matrix nets derive equivalent firing plans, dispatch the same transition handlers, and produce equivalent post-fire markings.

### Tests for User Story 1 ⚠️

> **NOTE: Write these properties FIRST, ensure they FAIL before implementation**

- [X] T011 [P] [US1] Add FsCheck.Xunit property for graph/matrix firing-plan equivalence in test/core.tests/FiringPlanProperties.cs
- [X] T012 [P] [US1] Add FsCheck.Xunit property for dispatcher-to-handler equivalence in test/core.tests/FiringPlanDispatcherProperties.cs
- [X] T013 [P] [US1] Add FsCheck.Xunit property for `GraphPetriNet.Fire` and `MatrixPetriNet.Fire` post-marking equivalence in test/core.tests/GraphPetriNetFireProperties.cs

### Implementation for User Story 1

- [X] T014 [US1] Implement shared firing-plan derivation in src/core/FiringPlanner.cs
- [X] T015 [US1] Implement shared firing-plan dispatch through transition handlers in src/core/FiringPlanDispatcher.cs
- [X] T016 [US1] Refactor `MatrixPetriNet` and `GraphPetriNet` to delegate firing-plan actioning to the shared dispatcher in src/core/MatrixPetriNet.cs and src/core/GraphPetriNet.cs
- [X] T017 [US1] Express shared firing-plan and dispatcher contracts idiomatically in src/core/FiringPlan.cs, src/core/FiringPlanDispatcher.cs, and src/core/PetriNetBase.cs

**Checkpoint**: Both net implementations derive the same firing plan abstraction and rely on the same dispatcher to turn plans into transition actions.

---

## Phase 4: User Story 2 - Faster Transition Selection Under Conflict (Priority: P1)

**Goal**: Optimize single-transition selection inside the shared firing plan semantics with a max-scan implementation.

**Independent Test**: Run the new FsCheck.Xunit property suites in test/core.tests/TransitionSelectionPriorityProperties.cs and test/core.tests/TransitionSelectionContractProperties.cs and verify highest-priority, first-seen tie, empty-plan, and default-zero priority invariants for generated selection candidates.

### Tests for User Story 2 ⚠️

- [X] T018 [P] [US2] Add FsCheck.Xunit property for highest-priority and first-seen tie invariants in test/core.tests/TransitionSelectionPriorityProperties.cs
- [X] T019 [P] [US2] Add FsCheck.Xunit property for empty enabled-set and default-zero priority invariants in test/core.tests/TransitionSelectionContractProperties.cs

### Implementation for User Story 2

- [X] T020 [US2] Implement the shared max-scan helper in src/core/TransitionSelection.cs
- [X] T021 [US2] Integrate the max-scan helper into shared firing-plan derivation in src/core/FiringPlanner.cs
- [X] T022 [US2] Express max-scan preconditions, null-result behavior, and tie-handling contracts idiomatically in src/core/TransitionSelection.cs

**Checkpoint**: The shared firing plan abstraction now uses max-scan selection semantics and remains independently testable.

---

## Phase 5: User Story 3 - Matrix Path Selection Parity (Priority: P1)

**Goal**: Verify that `MatrixPetriNet` preserves selection and firing-plan behavior after adopting the shared firing plan abstraction and max-scan selection.

**Independent Test**: Run the matrix property suites in test/core.tests/MatrixPetriNetProperties.cs and test/core.tests/MatrixPetriNetFiringPlanProperties.cs and verify parity for highest-priority selection, tie handling, null behavior, and conflict-driven firing-plan behavior.

### Tests for User Story 3 ⚠️

- [X] T023 [P] [US3] Add FsCheck.Xunit property for max-scan parity against current matrix selection semantics in test/core.tests/MatrixPetriNetProperties.cs
- [X] T024 [P] [US3] Add FsCheck.Xunit property for conflict firing-plan selection invariants in test/core.tests/MatrixPetriNetFiringPlanProperties.cs

### Implementation for User Story 3

- [X] T025 [US3] Integrate shared firing-plan derivation and dispatcher delegation into src/core/MatrixPetriNet.cs
- [X] T026 [US3] Express and document matrix selection and dispatcher-facing contracts idiomatically in src/core/MatrixPetriNet.cs
- [X] T027 [US3] Refactor matrix firing-path internals and remove obsolete model-specific actioning logic in src/core/MatrixPetriNet.cs

**Checkpoint**: `MatrixPetriNet` remains independently testable with property-based parity coverage under the shared firing plan abstraction.

---

## Phase 6: User Story 4 - Graph Path Selection Parity (Priority: P1)

**Goal**: Verify that `GraphPetriNet` preserves selection and firing behavior after adopting the shared firing plan abstraction and max-scan selection.

**Independent Test**: Run the graph property suites in test/core.tests/GraphPetriNetProperties.cs, test/core.tests/GraphPetriNetSelectionProperties.cs, and test/core.tests/GraphPetriNetFireProperties.cs and verify parity for highest-priority selection, tie handling, empty plans, and post-fire behavior.

### Tests for User Story 4 ⚠️

- [X] T028 [P] [US4] Add FsCheck.Xunit property for max-scan parity against current graph selection semantics in test/core.tests/GraphPetriNetProperties.cs
- [X] T029 [P] [US4] Add FsCheck.Xunit property for graph tie-handling and empty-plan invariants in test/core.tests/GraphPetriNetSelectionProperties.cs
- [X] T030 [P] [US4] Add FsCheck.Xunit property for graph post-fire equivalence in test/core.tests/GraphPetriNetFireProperties.cs

### Implementation for User Story 4

- [X] T031 [US4] Integrate shared firing-plan derivation and dispatcher delegation into src/core/GraphPetriNet.cs
- [X] T032 [US4] Express and document graph selection and dispatcher-facing contracts idiomatically in src/core/GraphPetriNet.cs
- [X] T033 [US4] Refactor graph firing-path internals and remove obsolete model-specific actioning logic in src/core/GraphPetriNet.cs

**Checkpoint**: `GraphPetriNet` remains independently testable with property-based parity coverage under the shared firing plan abstraction.

---

## Phase 7: User Story 5 - Reduced CPU in Selection Path (Priority: P2)

**Goal**: Add reproducible benchmark coverage proving the aligned shared firing plan and selection path meets the performance target without violating correctness-first rules.

**Independent Test**: Run `dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*TransitionSelection*"` and confirm both graph and matrix benchmarks execute using committed scenarios.

### Tests for User Story 5 ⚠️

- [X] T034 [P] [US5] Add FsCheck.Xunit property for deterministic benchmark scenario generation in test/core.tests/TransitionSelectionBenchmarkProperties.cs
- [X] T035 [P] [US5] Add FsCheck.Xunit property for graph and matrix benchmark fixture equivalence in test/core.tests/TransitionSelectionBenchmarkEquivalenceProperties.cs

### Implementation for User Story 5

- [X] T036 [US5] Implement benchmark scenario construction for large-net selection cases in perf/core.benchmarks/TransitionSelectionBenchmarkScenarios.cs
- [X] T037 [US5] Implement BenchmarkDotNet selection microbenchmarks in perf/core.benchmarks/TransitionSelectionBenchmarks.cs
- [X] T038 [US5] Wire benchmark discovery and runtime metadata in perf/core.benchmarks/Program.cs

**Checkpoint**: The performance story is independently verifiable from committed benchmark code.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Validate commands, sync the feature artifacts, and close out cross-story details.

- [X] T039 [P] Update final test and benchmark validation steps in specs/001-max-scan-selection/quickstart.md
- [X] T040 [P] Update final verification notes in specs/001-max-scan-selection/contracts/transition-selection-contract.md
- [X] T041 Run full regression validation with dotnet test petrinets2.slnx
- [X] T042 Validate benchmark project wiring and solution inclusion in petrinets2.slnx

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; starts immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational; establishes the shared firing plan abstraction and model equivalence.
- **User Story 2 (Phase 4)**: Depends on User Story 1 because max-scan must optimize the shared firing plan, not bypass it.
- **User Stories 3 and 4 (Phases 5 and 6)**: Depend on User Stories 1 and 2 because both models must consume the shared firing plan abstraction and shared max-scan selection.
- **User Story 5 (Phase 7)**: Depends on User Stories 3 and 4 because benchmarks must exercise the final aligned graph and matrix implementations.
- **Polish (Phase 8)**: Depends on all selected stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Independent after Foundational and serves as the MVP for shared firing-plan correctness.
- **User Story 2 (P1)**: Depends on User Story 1’s shared firing plan abstraction.
- **User Story 3 (P1)**: Depends on User Stories 1 and 2.
- **User Story 4 (P1)**: Depends on User Stories 1 and 2.
- **User Story 5 (P2)**: Depends on User Stories 3 and 4 so benchmark coverage reflects the completed aligned production paths.

### Within Each User Story

- FsCheck.Xunit properties MUST be written and fail before implementation begins.
- The first production change MUST be the minimum needed to make the failing properties pass.
- Contract expression and guard clauses must be updated with the production change, not deferred.
- Refactoring happens only after the relevant property suite is green.
- A story is complete only when its independent test criteria pass.

---

## Parallel Opportunities

- **Setup**: `T002` can run in parallel with `T001` once the benchmark directory exists.
- **Foundational**: `T009` and `T010` can run in parallel because property generators and benchmark builders live in separate files after the firing plan and dispatcher scaffold exists.
- **User Story 1**: `T011`, `T012`, and `T013` can run in parallel because they target separate property files.
- **User Story 2**: `T018` and `T019` can run in parallel because they target separate property files.
- **User Story 3**: `T023` and `T024` can run in parallel because they cover separate matrix invariants.
- **User Story 4**: `T028`, `T029`, and `T030` can run in parallel because they target separate graph property files.
- **User Story 5**: `T034` and `T035` can run in parallel because they target separate benchmark property files.
- **Polish**: `T039` and `T040` can run in parallel because they touch separate feature documents.

### Parallel Example: User Story 1

```bash
Task: "Add FsCheck.Xunit property for graph/matrix firing-plan equivalence in test/core.tests/FiringPlanProperties.cs"
Task: "Add FsCheck.Xunit property for dispatcher-to-handler equivalence in test/core.tests/FiringPlanDispatcherProperties.cs"
Task: "Add FsCheck.Xunit property for GraphPetriNet.Fire and MatrixPetriNet.Fire post-marking equivalence in test/core.tests/GraphPetriNetFireProperties.cs"
```

### Parallel Example: User Story 2

```bash
Task: "Add FsCheck.Xunit property for highest-priority and first-seen tie invariants in test/core.tests/TransitionSelectionPriorityProperties.cs"
Task: "Add FsCheck.Xunit property for empty enabled-set and default-zero priority invariants in test/core.tests/TransitionSelectionContractProperties.cs"
```

### Parallel Example: User Story 3

```bash
Task: "Add FsCheck.Xunit property for max-scan parity against current matrix selection semantics in test/core.tests/MatrixPetriNetProperties.cs"
Task: "Add FsCheck.Xunit property for conflict firing-plan selection invariants in test/core.tests/MatrixPetriNetFiringPlanProperties.cs"
```

### Parallel Example: User Story 4

```bash
Task: "Add FsCheck.Xunit property for max-scan parity against current graph selection semantics in test/core.tests/GraphPetriNetProperties.cs"
Task: "Add FsCheck.Xunit property for graph tie-handling and empty-plan invariants in test/core.tests/GraphPetriNetSelectionProperties.cs"
```

### Parallel Example: User Story 5

```bash
Task: "Add FsCheck.Xunit property for deterministic benchmark scenario generation in test/core.tests/TransitionSelectionBenchmarkProperties.cs"
Task: "Add FsCheck.Xunit property for graph and matrix benchmark fixture equivalence in test/core.tests/TransitionSelectionBenchmarkEquivalenceProperties.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Setup and Foundational phases.
2. Deliver User Story 1 by proving the shared firing plan abstraction with property-based tests.
3. Stop and validate shared step semantics independently before optimizing selection.

### Incremental Delivery

1. Finish Setup + Foundational.
2. Deliver User Story 1 and validate shared firing-plan equivalence.
3. Deliver User Story 2 and validate max-scan behavior inside the shared abstraction.
4. Deliver User Story 3 and validate matrix parity.
5. Deliver User Story 4 and validate graph parity.
6. Deliver User Story 5 and validate benchmark evidence.
7. Finish with Polish tasks to sync quickstart, contract artifacts, and full regression validation.

### Parallel Team Strategy

1. One developer creates benchmark/setup scaffolding while another builds property generators in Phase 2.
2. After User Stories 1 and 2 land, matrix and graph integrations can proceed in parallel.
3. Once both implementations are complete, benchmark and documentation tasks can run in parallel.

---

## Notes

- All tasks use the required checklist format with IDs, optional parallel markers, story labels, and exact file paths.
- User stories are ordered by the current spec priority and dependency structure.
- The constitution-driven constraints are embedded directly into the task order: properties first, minimum implementation second, refactor third.
- Suggested MVP scope: User Story 1 only, because it establishes the canonical shared firing plan semantics and the single dispatcher path for all later performance work.
