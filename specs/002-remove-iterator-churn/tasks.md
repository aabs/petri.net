# Tasks: Hot-Path Allocation Removal

**Input**: Design documents from `/specs/002-remove-iterator-churn/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Property-based tests are required for this feature. Every hotspot slice must begin with FsCheck/FsCheck.Xunit properties that are written first, observed failing, and only then followed by production changes. Benchmark and profiling tasks are also required because performance is part of the acceptance criteria.

**Organization**: Tasks are grouped by user story to keep the R2 work independently implementable and measurable, with shared scaffolding and validation phases around it.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the shared scenario and benchmark scaffolding needed to exercise the R2 hotspots reproducibly.

- [X] T001 Create shared hotspot property data builders in test/core.tests/HotPathAllocationPropertyData.cs
- [X] T002 [P] Create shared hotspot benchmark scenario builders in perf/core.benchmarks/HotPathAllocationBenchmarkScenarios.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the benchmark and verification scaffolding that all hotspot changes depend on.

**⚠️ CRITICAL**: No hotspot implementation work should begin until this phase is complete.

- [X] T003 Create deterministic benchmark-scenario property coverage in test/core.tests/HotPathAllocationBenchmarkProperties.cs
- [X] T004 [P] Create benchmark fixture parity coverage between graph and matrix scenarios in test/core.tests/HotPathAllocationBenchmarkEquivalenceProperties.cs
- [X] T005 Create the initial BenchmarkDotNet entry shell and baseline benchmark registration for the R2 conflict and fire hotspots in perf/core.benchmarks/HotPathAllocationBenchmarks.cs

**Checkpoint**: Shared R2 test data and benchmark scaffolding are ready; hotspot slices can now be implemented with reproducible parity and allocation validation.

---

## Phase 3: User Story 1 - Reduce runtime allocation pressure (Priority: P1) 🎯 MVP

**Goal**: Remove avoidable materialization and iterator churn from the R2 hotspots in `PetriNetBase`, `GraphPetriNet`, and `MatrixPetriNet` while preserving exact enablement, firing, token-update, and dispatch behavior.

**Independent Test**: Run the FsCheck/Xunit hotspot parity suites plus the committed R2 benchmarks and verify unchanged behavior together with reduced allocations in the targeted conflict and fire paths.

### Tests for User Story 1 ⚠️

> **NOTE: Write these properties FIRST, ensure they FAIL before implementation**

- [X] T006 [P] [US1] Add FsCheck.Xunit properties for conflict parity and adjacent-enabled parity in test/core.tests/PetriNetBaseProperties.cs
- [X] T007 [P] [US1] Add FsCheck.Xunit properties for graph fire-result parity and transition-function invocation-order parity in test/core.tests/GraphPetriNetFireProperties.cs
- [X] T008 [P] [US1] Add FsCheck.Xunit properties for matrix fire-result parity and transition-function invocation-order parity in test/core.tests/MatrixPetriNetFiringPlanProperties.cs
- [X] T009 [P] [US1] Add FsCheck.Xunit properties for graph and matrix enablement parity under repeated-traversal cases in test/core.tests/GraphPetriNetSelectionProperties.cs and test/core.tests/MatrixPetriNetProperties.cs

### Implementation for User Story 1

- [X] T010 [US1] Remove the `ToArray()` plus `Count() > 1` conflict path and implement an early-exit non-allocating conflict helper in src/core/PetriNetBase.cs
- [X] T011 [P] [US1] Replace hot-path `Where`/`Select` iterator chains with direct loops and local references in src/core/GraphPetriNet.cs
- [X] T012 [P] [US1] Replace matrix hot-path enumerable helpers and per-iteration helper-call overhead with explicit loops in src/core/MatrixPetriNet.cs
- [X] T013 [US1] Extend hotspot benchmark scenarios for conflict, graph fire, and matrix fire workloads in perf/core.benchmarks/HotPathAllocationBenchmarkScenarios.cs
- [X] T014 [US1] Complete BenchmarkDotNet coverage for the R2 conflict and fire hotspots in perf/core.benchmarks/HotPathAllocationBenchmarks.cs using the shared scenario builders and update perf/core.benchmarks/Program.cs if benchmark discovery requires it
- [X] T015 [US1] Update R2 behavior and verification notes in specs/002-remove-iterator-churn/contracts/hot-path-allocation-contract.md and specs/002-remove-iterator-churn/quickstart.md once the hotspot properties are green

**Checkpoint**: The R2 hotspots have property-backed parity coverage, benchmark coverage, and concrete allocation-reduction changes in the scoped production files.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Run full validation, capture evidence, and close the feature with reproducible commands.

- [X] T016 [P] Record full regression validation for the R2 hotspot files in specs/002-remove-iterator-churn/quickstart.md after running `dotnet test petrinets2.slnx`
- [X] T017 [P] Record focused R2 benchmark validation for perf/core.benchmarks/HotPathAllocationBenchmarks.cs in specs/002-remove-iterator-churn/quickstart.md after running `dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*HotPathAllocation*|*Conflict*|*Fire*"`
- [X] T018 Capture hotspot-specific allocation evidence, record `Marking` copy cost as supporting context, and update specs/002-remove-iterator-churn/quickstart.md and specs/002-remove-iterator-churn/contracts/hot-path-allocation-contract.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; starts immediately.
- **Foundational (Phase 2)**: Depends on Setup and blocks all hotspot implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational; delivers the full feature outcome because the spec contains one user story.
- **Polish (Phase 4)**: Depends on User Story 1 completion.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Phase 2 and contains all R2 hotspot changes. There are no additional user stories because semantic parity and rollout sequencing are treated as cross-cutting constraints rather than separate feature slices.

### Within User Story 1

- FsCheck.Xunit properties in T006 through T009 MUST be written and observed failing before T010 through T015 begin.
- T010 should land before or alongside the conflict benchmark scenario in T013 because the base conflict-path change is the clearest isolated hotspot.
- T011 and T012 can proceed independently after the shared test scaffolding is ready because they target different production files.
- T013 must precede T014 because the benchmark implementation depends on the scenario builders.
- T015 should happen only after the production and benchmark work is green so the docs reflect the actual delivered validation flow.

---

## Parallel Opportunities

- T001 and T002 can run in parallel because property-data scaffolding and benchmark-scenario scaffolding live in different files.
- T003 and T004 can run in parallel once T001 and T002 exist.
- T006 through T009 can run in parallel because they target separate property files.
- T011 and T012 can run in parallel after the failing properties are in place because they touch different production files.
- T016 and T017 can run in parallel after implementation completes.

### Parallel Example: Property Tasks

```bash
Task: "Add FsCheck.Xunit properties for conflict parity and adjacent-enabled parity in test/core.tests/PetriNetBaseProperties.cs"
Task: "Add FsCheck.Xunit properties for graph fire-result parity and transition-function invocation-order parity in test/core.tests/GraphPetriNetFireProperties.cs"
Task: "Add FsCheck.Xunit properties for matrix fire-result parity and transition-function invocation-order parity in test/core.tests/MatrixPetriNetFiringPlanProperties.cs"
Task: "Add FsCheck.Xunit properties for graph and matrix enablement parity under repeated-traversal cases in test/core.tests/GraphPetriNetSelectionProperties.cs and test/core.tests/MatrixPetriNetProperties.cs"
```

### Parallel Example: Production Tasks

```bash
Task: "Replace hot-path iterator chains with direct loops in src/core/GraphPetriNet.cs"
Task: "Replace matrix hot-path enumerable helpers with explicit loops in src/core/MatrixPetriNet.cs"
```

---

## Implementation Strategy

### MVP First (Single Story)

1. Complete Phase 1 and Phase 2 to establish reusable test and benchmark scaffolding.
2. Write the hotspot parity properties in Phase 3 and confirm they fail.
3. Implement the `PetriNetBase`, `GraphPetriNet`, and `MatrixPetriNet` hotspot reductions.
4. Add the R2 benchmark coverage and validate the allocation target.
5. Stop and validate the single user story independently.

### Incremental Delivery Inside the Story

1. Land the base conflict-path rewrite in src/core/PetriNetBase.cs with its parity properties.
2. Land the graph hot-loop rewrite in src/core/GraphPetriNet.cs with graph parity checks.
3. Land the matrix hot-loop rewrite in src/core/MatrixPetriNet.cs with matrix parity checks.
4. Land benchmark scenario and benchmark implementation updates in perf/core.benchmarks.
5. Finish with full regression and recorded allocation evidence.

### Parallel Team Strategy

1. One developer can build the R2 benchmark scaffolding while another builds the shared property data.
2. After the failing properties exist, graph and matrix hotspot rewrites can proceed in parallel.
3. Benchmark implementation and documentation updates can proceed once the production hotspots are green.

---

## Notes

- This feature intentionally has one user story because the real user-facing outcome is allocation reduction in steady-state execution; the hotspot slices are implementation slices inside that story.
- The task order enforces the repository constitution: property first, minimum production change second, refactor and documentation last.
- The benchmark tasks are mandatory, not optional polish, because the current committed perf harness does not yet validate R2.