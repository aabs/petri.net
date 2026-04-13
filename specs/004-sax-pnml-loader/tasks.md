# Tasks: Streamed PNML Loader via XmlReader

**Input**: Design documents from /specs/004-sax-pnml-loader/
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/pnml-streaming-loader-contract.md, quickstart.md

**Tests**: Property-based tests are REQUIRED. For each behavior cluster, write FsCheck/FsCheck.Xunit properties first, observe failure, then implement the minimum code to pass.

**Organization**: Tasks are grouped for independent execution: setup, foundational, core user story delivery (US1), then polish/cross-cutting validation.

## Format: [ID] [P?] [Story] Description

- [P]: Task can run in parallel (different files, no blocking dependency)
- [Story]: Story label for traceability (`US1` in this feature)
- Every task includes exact file paths

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare benchmark and test scaffolding for streamed loader + new builder work.

- [ ] T001 Create builder and loader property data generators in test/core.tests/PnmlStreamingPropertyData.cs
- [ ] T002 [P] Create benchmark scenario generator for streamed PNML inputs in perf/core.benchmarks/PnmlLoadBenchmarkScenarios.cs
- [ ] T003 [P] Create benchmark scenario generator for builder construction pressure in perf/core.benchmarks/PetriNetBuilderBenchmarkScenarios.cs
- [ ] T004 [P] Register new FsCheck arbitraries for PNML/builder property inputs in test/core.tests/globals.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish API contracts and baseline test harnesses that all implementation tasks depend on.

**CRITICAL**: Do not begin production implementation before this phase is complete.

- [ ] T005 Add failing FsCheck.Xunit contract-focused properties for `PetriNetBuilder` fluent API invariants in test/core.tests/PetriNetBuilderProperties.cs
- [ ] T006 [P] Add failing FsCheck.Xunit contract-focused properties for streamed loader ordering and multi-net behavior in test/core.tests/PnmlStreamingLoaderProperties.cs
- [ ] T007 [P] Add failing FsCheck.Xunit property suites for core PT-net conformance/failure-mode invariants in test/core.tests/PnmlStreamingLoaderConformanceTests.cs
- [ ] T008 Add benchmark harness shell for builder and streamed loader with `MemoryDiagnoser` in perf/core.benchmarks/PnmlStreamingLoadBenchmarks.cs
- [ ] T009 [P] Register streamed and builder benchmarks in perf/core.benchmarks/Program.cs

**Checkpoint**: Foundation complete; user story implementation can begin.

---

## Phase 3: User Story 1 - Build Nets While Streaming PNML (Priority: P1) 🎯 MVP

**Goal**: Deliver a single-pass `XmlReader` loader that constructs nets through a new conventional `PetriNetBuilder`, supports Graph/Matrix outputs, preserves behavior, and reduces memory pressure.

**Independent Test**: Load embedded sample corpus and generated PNML inputs through new loader in both Graph and Matrix modes, assert parity and deterministic failures, and verify benchmark deltas for throughput/allocations.

### Tests for User Story 1 (write first)

- [ ] T010 [P] [US1] Add failing property for sparse-to-dense parity (`BuildGraph` vs `BuildMatrix`) in test/core.tests/PetriNetBuilderProperties.cs
- [ ] T011 [P] [US1] Add failing property for builder endpoint/index validity invariants in test/core.tests/PetriNetBuilderProperties.cs
- [ ] T012 [P] [US1] Add failing property for streamed loader graph/matrix behavioral parity in test/core.tests/PnmlStreamingLoaderProperties.cs
- [ ] T013 [P] [US1] Add failing property for streamed loader preserving source net order in multi-net documents in test/core.tests/PnmlStreamingLoaderProperties.cs
- [ ] T014 [P] [US1] Add failing FsCheck.Xunit conformance properties that reuse embedded sample corpus across input classes in test/core.tests/PnmlStreamingLoaderConformanceTests.cs
- [ ] T015 [P] [US1] Add failing FsCheck.Xunit failure-contract properties for duplicate IDs and missing arc endpoints in test/core.tests/PnmlStreamingLoaderConformanceTests.cs

### Implementation for User Story 1

- [ ] T016 [US1] Implement new fluent builder class `PetriNetBuilder` with `With*` and `Adding*` idioms in src/core/builders/PetriNetBuilder.cs
- [ ] T017 [US1] Implement sparse intermediate adjacency state and compact projection to dense graph model in src/core/builders/PetriNetBuilder.cs
- [ ] T018 [US1] Implement compact projection to dense matrix model with minimal temporary allocations in src/core/builders/PetriNetBuilder.cs
- [ ] T019 [US1] Implement single-pass `XmlReader` loader using `PetriNetBuilder` in src/core/PnmlStreamingModelLoader.cs
- [ ] T020 [US1] Add Graph/Matrix target selection API and route finalization through `PetriNetBuilder` terminal build methods in src/core/PnmlStreamingModelLoader.cs
- [ ] T021 [US1] Add deterministic exception messaging and guard clauses for malformed core PT-net inputs in src/core/PnmlStreamingModelLoader.cs
- [ ] T022 [US1] Mark existing loader as deprecated without behavior change in src/core/PnmlModelLoader.cs
- [ ] T023 [US1] Document migration guidance from old loader to streamed loader and any builder API deltas in docs/the-arc-language.md

**Checkpoint**: US1 complete and independently testable.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Validate success criteria, complete benchmark evidence, and align documentation/contracts.

- [ ] T024 [P] Add builder-focused benchmark cases and allocation metrics in perf/core.benchmarks/PetriNetBuilderBenchmarks.cs
- [ ] T025 [P] Add old-vs-new loader benchmark cases in perf/core.benchmarks/PnmlStreamingLoadBenchmarks.cs
- [ ] T026 [P] Capture benchmark evidence required by SC-001/SC-002 in specs/004-sax-pnml-loader/quickstart.md
- [ ] T027 [P] Record conformance and regression command outcomes in specs/004-sax-pnml-loader/quickstart.md
- [ ] T028 Update contract details for any finalized API names/signatures in specs/004-sax-pnml-loader/contracts/pnml-streaming-loader-contract.md
- [ ] T029 Verify plan completion checklist/status notes in specs/004-sax-pnml-loader/plan.md
- [ ] T030 Run and gate all existing affected property suites for loading/firing invariants and fail completion on any regression in test/core.tests

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 has no dependencies.
- Phase 2 depends on Phase 1 and blocks story implementation.
- Phase 3 depends on Phase 2.
- Phase 4 depends on Phase 3 completion.

### Within User Story 1

- Write property/conformance tests first (T010-T015) and confirm failures.
- Implement builder and loader minimum changes (T016-T022).
- Update migration docs once implementation behavior is stable (T023).

### Parallel Opportunities

- Phase 1: T002, T003, T004 in parallel.
- Phase 2: T006, T007, T009 in parallel after T005/T008 baseline scaffolding.
- Phase 3 tests: T010-T015 in parallel.
- Phase 4: T024-T027 in parallel.

---

## Implementation Strategy

### MVP First

1. Complete Phases 1 and 2.
2. Complete US1 tasks through T022.
3. Validate with tests and sample corpus.
4. Proceed to benchmark/documentation polish.

### Incremental Delivery

1. Deliver `PetriNetBuilder` + loader skeleton.
2. Deliver parity and failure correctness.
3. Deliver performance/allocation improvements.
4. Finalize docs and contracts.

---

## Notes

- This feature has one prioritized user story; conformance and regression obligations are captured as acceptance and validation tasks, not separate stories.
- Keep old `PnmlModelLoader` behavior stable while introducing deprecation warnings.
- Preserve constitution constraints: property-based TDD, correctness before performance, and idiomatic C# contracts.
