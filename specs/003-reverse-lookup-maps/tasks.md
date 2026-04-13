# Tasks: Reverse Lookup Maps for Names to Indices

**Input**: Design documents from /specs/003-reverse-lookup-maps/
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Property-based tests are required for this feature. Every user story includes FsCheck/FsCheck.Xunit properties that must be written first, observed failing, and only then followed by implementation tasks. Benchmark tasks are required because performance outcomes are part of success criteria.

**Organization**: Tasks are grouped by user story to keep each story independently implementable and testable.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create shared scaffolding for deterministic property generation and benchmark scenarios.

- [X] T001 Create reverse-lookup property scenario builders in test/core.tests/ReverseLookupPropertyData.cs
- [X] T002 [P] Create construction benchmark scenario builders in perf/core.benchmarks/ReverseLookupConstructionBenchmarkScenarios.cs
- [X] T003 [P] Create PNML benchmark scenario builders in perf/core.benchmarks/PnmlLoadBenchmarkScenarios.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared benchmark and property-test plumbing that blocks all user stories.

**CRITICAL**: No user story implementation should begin until this phase is complete.

- [X] T004 Create shared reverse-lookup benchmark harness shell with MemoryDiagnoser in perf/core.benchmarks/ReverseLookupBenchmarks.cs
- [X] T005 [P] Register reverse-lookup benchmarks in perf/core.benchmarks/Program.cs
- [X] T006 [P] Register reverse-lookup FsCheck arbitraries in test/core.tests/globals.cs
- [X] T007 Create benchmark-scenario parity properties for shared construction/PNML fixtures in test/core.tests/ReverseLookupBenchmarkProperties.cs

**Checkpoint**: Shared property and benchmark infrastructure is ready for independent user story delivery.

---

## Phase 3: User Story 1 - O(1) Name-to-Index Resolution During Net Construction (Priority: P1) 🎯 MVP

**Goal**: Replace linear place/transition name resolution in builder lookup methods with O(1) reverse-map lookups while preserving public API and behavior contracts.

**Independent Test**: Build nets at small/medium/large scales via builder API and verify `PlaceIndex` and `TransitionIndex` always match forward maps, preserve failure behavior for missing names, and remain consistent under concurrent lookup and arc-add workloads.

### Tests for User Story 1

- [X] T008 [P] [US1] Add FsCheck.Xunit property for place lookup parity with forward map in test/core.tests/CreatePetriNetReverseLookupProperties.cs
- [X] T009 [P] [US1] Add FsCheck.Xunit property for transition lookup parity with forward map in test/core.tests/CreatePetriNetReverseLookupProperties.cs
- [X] T010 [P] [US1] Add FsCheck.Xunit property for missing-name failure contracts in test/core.tests/CreatePetriNetReverseLookupProperties.cs
- [X] T034 [P] [US1] Add FsCheck.Xunit property for concurrent `PlaceIndex`/`TransitionIndex` and arc-add consistency in test/core.tests/CreatePetriNetReverseLookupProperties.cs

### Implementation for User Story 1

- [X] T011 [US1] Add `_placesByName` and `_transitionsByName` state initialization in src/core/builders/CreatePetriNet.cs
- [X] T012 [US1] Implement guard-based O(1) `PlaceIndex` and `TransitionIndex` lookups in src/core/builders/CreatePetriNet.cs
- [X] T013 [US1] Add reverse-map rebuild/synchronization helper for forward-map consistency paths in src/core/builders/CreatePetriNet.cs
- [X] T035 [US1] Add synchronization strategy for concurrent lookup/mutation paths in src/core/builders/CreatePetriNet.cs
- [X] T014 [US1] Update builder lookup contract notes for missing-name and concurrency behavior in specs/003-reverse-lookup-maps/contracts/reverse-lookup-contract.md

**Checkpoint**: User Story 1 is independently functional and testable with property evidence for constant-time name resolution behavior.

---

## Phase 4: User Story 2 - O(1) Duplicate Detection in WithPlaces and WithTransitions (Priority: P2)

**Goal**: Remove remaining O(n) duplicate checks from `WithPlaces` and `WithTransitions` using reverse-map key lookups while preserving duplicate-ignore semantics.

**Independent Test**: Register mixed new/duplicate names in repeated calls and verify only new names are added, original indices remain stable, and forward/reverse maps remain bijectively consistent.

### Tests for User Story 2

- [X] T015 [P] [US2] Add FsCheck.Xunit property for `WithPlaces` duplicate-ignore and index-stability invariants in test/core.tests/CreatePetriNetReverseLookupProperties.cs
- [X] T016 [P] [US2] Add FsCheck.Xunit property for `WithTransitions` duplicate-ignore and index-stability invariants in test/core.tests/CreatePetriNetReverseLookupProperties.cs
- [X] T017 [P] [US2] Add FsCheck.Xunit property for forward/reverse map bijection after mixed builder operations in test/core.tests/CreatePetriNetReverseLookupProperties.cs

### Implementation for User Story 2

- [X] T018 [US2] Replace place duplicate detection with `_placesByName` key lookup in src/core/builders/CreatePetriNet.cs
- [X] T019 [US2] Replace transition duplicate detection with `_transitionsByName` key lookup in src/core/builders/CreatePetriNet.cs
- [X] T020 [US2] Ensure reverse and forward map updates happen atomically within insertion loops in src/core/builders/CreatePetriNet.cs
- [X] T021 [US2] Update duplicate-detection contract details in specs/003-reverse-lookup-maps/contracts/reverse-lookup-contract.md

**Checkpoint**: User Story 2 is independently functional and testable with O(1) duplicate checks and verified map consistency invariants.

---

## Phase 5: User Story 3 - PNML Loader Uses Reverse Maps for Place Lookups (Priority: P2)

**Goal**: Replace PNML place-id value scans with O(1) place-id lookup maps in both `Load` and `LoadMarkings`, preserving result parity and actionable error behavior.

**Independent Test**: Load PNML models at increasing size and verify correct arc/marking index resolution, stable behavior parity, and clear missing-place-id failures.

### Tests for User Story 3

- [X] T022 [P] [US3] Add FsCheck.Xunit property for PNML arc place-id resolution parity in test/core.tests/PnmlModelLoaderReverseLookupProperties.cs
- [X] T023 [P] [US3] Add FsCheck.Xunit property for `LoadMarkings` place-id resolution parity and failure contracts in test/core.tests/PnmlModelLoaderReverseLookupProperties.cs
- [X] T024 [P] [US3] Add FsCheck.Xunit property validating deterministic PNML benchmark fixture parity in test/core.tests/ReverseLookupBenchmarkProperties.cs

### Implementation for User Story 3

- [X] T025 [US3] Introduce per-net place-id-to-index lookup map in PNML load flow in src/core/PnmlModelLoader.cs
- [X] T026 [US3] Replace value-scan place resolution in `LoadMarkings` with lookup-map resolution in src/core/PnmlModelLoader.cs
- [X] T027 [US3] Implement `KeyNotFoundException` for missing place IDs with actionable context in src/core/PnmlModelLoader.cs
- [X] T028 [US3] Add PNML load benchmark measurements for reverse-lookup path in perf/core.benchmarks/ReverseLookupBenchmarks.cs
- [X] T029 [US3] Update PNML resolution contract notes for lookup behavior and failure modes in specs/003-reverse-lookup-maps/contracts/reverse-lookup-contract.md

**Checkpoint**: User Story 3 is independently functional and testable with O(1) PNML place-id resolution and benchmark coverage.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate full feature outcomes, capture reproducible evidence, and finalize documentation.

- [X] T030 [P] Add construction benchmark coverage and large-scale parameters for SC-002 in perf/core.benchmarks/ReverseLookupBenchmarks.cs
- [X] T031 [P] Record full feature validation commands and expected checks in specs/003-reverse-lookup-maps/quickstart.md
- [X] T032 Record benchmark evidence expectations for SC-002/SC-003/SC-006 in specs/003-reverse-lookup-maps/quickstart.md
- [X] T033 [P] Update feature summary and completion checklist in specs/003-reverse-lookup-maps/plan.md
- [X] T036 Verify FR-009 API signature compatibility for builder fluent and lookup methods against baseline in src/core/builders/CreatePetriNet.cs
- [X] T037 Run full regression suite for SC-004 with dotnet test petrinets2.slnx and record pass evidence in specs/003-reverse-lookup-maps/quickstart.md
- [X] T038 Confirm clone and serialize remain out of scope and document future reconstruction rule in specs/003-reverse-lookup-maps/spec.md and specs/003-reverse-lookup-maps/contracts/reverse-lookup-contract.md

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1): no dependencies.
- Foundational (Phase 2): depends on Phase 1 and blocks all user stories.
- User Story phases (Phases 3 to 5): all depend on Phase 2 completion.
- Polish (Phase 6): depends on completion of all user story phases.

### User Story Dependencies

- US1 (P1): starts after Phase 2; no dependency on other user stories.
- US2 (P2): starts after US1 because duplicate checks rely on reverse-map infrastructure introduced in US1.
- US3 (P2): starts after US1; independent of US2.

### Within Each User Story

- Write property tasks first and confirm they fail for the expected reason.
- Implement the minimal production change to satisfy properties.
- Refactor after properties are green.
- Keep contract and quickstart docs aligned with delivered behavior.

---

## Parallel Opportunities

- T002 and T003 can run in parallel in Setup (different benchmark files).
- T005, T006, and T007 can run in parallel after T004.
- In US1, T008, T009, T010, and T034 can run in parallel.
- In US2, T015, T016, and T017 can run in parallel.
- In US3, T022, T023, and T024 can run in parallel.
- In Polish, T030, T031, T033, and T036 can run in parallel.

### Parallel Example: User Story 1

```bash
Task: "Add FsCheck.Xunit property for place lookup parity in test/core.tests/CreatePetriNetReverseLookupProperties.cs"
Task: "Add FsCheck.Xunit property for transition lookup parity in test/core.tests/CreatePetriNetReverseLookupProperties.cs"
Task: "Add FsCheck.Xunit property for missing-name failure contracts in test/core.tests/CreatePetriNetReverseLookupProperties.cs"
Task: "Add FsCheck.Xunit property for concurrent lookup/mutation consistency in test/core.tests/CreatePetriNetReverseLookupProperties.cs"
```

### Parallel Example: User Story 2

```bash
Task: "Add FsCheck.Xunit property for WithPlaces duplicate/index invariants in test/core.tests/CreatePetriNetReverseLookupProperties.cs"
Task: "Add FsCheck.Xunit property for WithTransitions duplicate/index invariants in test/core.tests/CreatePetriNetReverseLookupProperties.cs"
Task: "Add FsCheck.Xunit property for forward/reverse bijection invariants in test/core.tests/CreatePetriNetReverseLookupProperties.cs"
```

### Parallel Example: User Story 3

```bash
Task: "Add FsCheck.Xunit property for PNML arc place-id resolution parity in test/core.tests/PnmlModelLoaderReverseLookupProperties.cs"
Task: "Add FsCheck.Xunit property for LoadMarkings resolution/failure contracts in test/core.tests/PnmlModelLoaderReverseLookupProperties.cs"
Task: "Add deterministic PNML benchmark fixture parity property in test/core.tests/ReverseLookupBenchmarkProperties.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete US1 test-first flow in Phase 3.
3. Validate US1 independently with property suite and focused benchmarks.
4. Pause for review/demo before expanding to P2 stories.

### Incremental Delivery

1. Deliver US1 (core reverse lookup) as MVP.
2. Deliver US2 (duplicate detection) without changing public API.
3. Deliver US3 (PNML lookup path) with benchmark evidence.
4. Complete polish tasks for reproducible validation and closure.

### Parallel Team Strategy

1. One developer handles builder reverse-map work (US1 + US2) while another prepares PNML property fixtures.
2. After US1 lands, US2 and US3 can proceed in parallel.
3. Final benchmark/doc tasks can be split across team members.

---

## Notes

- Task format strictly follows the required checklist syntax: checkbox, task ID, optional [P], required [USx] in story phases, and exact file path.
- Public API signatures listed in FR-009 are intentionally preserved across all implementation tasks.
- T036 is the explicit FR-009 verification gate and T037 is the explicit SC-004 regression gate.
- Property-first order is mandatory for every user story in this feature.
