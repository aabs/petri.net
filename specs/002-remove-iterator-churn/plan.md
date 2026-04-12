# Implementation Plan: Hot-Path Allocation Removal

**Branch**: `002-remove-iterator-churn` | **Date**: 2026-04-12 | **Spec**: `/Users/aabs/dev/aabs/active/computational-models/petri.net/specs/002-remove-iterator-churn/spec.md`
**Input**: Feature specification from `/specs/002-remove-iterator-churn/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Implement R2 by removing avoidable materialization and iterator churn from the actual steady-state execution paths identified in the remediation analysis and visible in the current code. The main targets are the conflict and enablement helpers in `PetriNetBase`, the LINQ-heavy enablement and firing paths in `GraphPetriNet`, and the dense matrix fire and enablement helpers in `MatrixPetriNet`, with `Marking` copy/materialization treated as a measured supporting cost rather than an excuse to introduce in-place semantics. The plan keeps one hard boundary throughout: preserve exact enablement results, firing outcomes, token updates, and transition-function invocation order while reducing bytes per operation in the firing path and making the allocation call tree materially smaller at the named hotspots.

## Technical Context

**Language/Version**: C# 14 on .NET 10  
**Primary Dependencies**: `MathNet.Numerics` in `src/core`; `xUnit`, `FsCheck`, and `FsCheck.Xunit` in `test/core.tests`; `BenchmarkDotNet` in `perf/core.benchmarks`  
**Storage**: N/A  
**Testing**: `dotnet test` with FsCheck.Xunit property-based tests and BenchmarkDotNet microbenchmarks with memory diagnostics  
**Target Platform**: .NET 10 on macOS, Linux, and Windows  
**Project Type**: Multi-project .NET library repository  
**Performance Goals**: Preserve exact behavior first, then reduce bytes per operation by at least 30% in representative firing benchmarks and, where a representative load replay harness already exists, improve p99 latency by at least 10% in representative load replay  
**Constraints**: No public API breaks; no token-update semantic changes; no new deprecated Code Contracts usage; no expansion into reverse lookup maps, PNML traversal redesign, sparse matrix kernel redesign, or in-place marking semantics changes  
**Scale/Scope**: Production edits are limited to the R2 hotspot paths in `src/core`, supported by tests in `test/core.tests` and extended benchmark coverage in `perf/core.benchmarks`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- C# 14 on .NET 10: PASS. All planned edits remain within the mandated language/runtime target.
- Correctness before performance: PASS. The feature is organized around explicit parity constraints, and performance work is accepted only after unchanged behavior is proven.
- Property-based TDD: PASS. Each hotspot slice will start with failing FsCheck/FsCheck.Xunit properties or equivalent parity properties that describe behavioral classes.
- Behavioral properties, not disguised examples: PASS. The new or extended properties target enablement parity, conflict parity, firing parity, and dispatch-order parity over generated nets and markings.
- Idiomatic contracts, no new Code Contracts: PASS. The implementation avoids expanding the existing Code Contracts surface, especially in `MatrixPetriNet`, and keeps contract expression in idiomatic C# where touched.

Post-design re-check: PASS. The design remains narrowly focused on R2 hotspot behavior and defers delivery slicing, PR sequencing, and acceptance batching to `/speckit.tasks`.

## Project Structure

### Documentation (this feature)

```text
specs/002-remove-iterator-churn/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── hot-path-allocation-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── core/
│   ├── GraphPetriNet.cs
│   ├── Marking.cs
│   ├── MatrixPetriNet.cs
│   ├── PetriNetBase.cs
│   ├── FiringPlan.cs
│   ├── FiringPlanner.cs
│   └── petrinets2.core.csproj
└── arclang/
  └── arclang.csproj

test/
└── core.tests/
  ├── core.tests.csproj
  ├── GraphPetriNetFireProperties.cs
  ├── GraphPetriNetSelectionProperties.cs
  ├── MatrixPetriNetFiringPlanProperties.cs
  ├── MatrixPetriNetProperties.cs
  ├── PetriNetBaseProperties.cs
  ├── TransitionSelectionBenchmarkEquivalenceProperties.cs
  ├── TransitionSelectionBenchmarkProperties.cs
  └── TransitionSelectionContractProperties.cs

perf/
└── core.benchmarks/
  ├── Program.cs
  ├── TransitionSelectionBenchmarkScenarios.cs
  ├── TransitionSelectionBenchmarks.cs
  └── core.benchmarks.csproj
```

**Structure Decision**: Keep the existing repository layout. Modify the hotspot production logic in `src/core`, extend the existing property suites in `test/core.tests`, and extend the existing committed BenchmarkDotNet project in `perf/core.benchmarks` instead of creating a second performance harness.

## Hotspot Inventory

### 1. Base conflict and enablement helpers in `PetriNetBase`

- Current code:
  - `IsConflicted(Marking m)` calls `AllPlaces().Any(pid => PlaceIsConflicted(pid, m))`.
  - `PlaceIsConflicted(int placeId, Marking m)` calls `GetEnabledTransitionsAdjacentToPlace(placeId, m).Count() > 1`.
  - `GetEnabledTransitionsAdjacentToPlace(int placeId, Marking m)` materializes `ToArray()` after filtering `GetPlaceOutArcs(placeId)` through `IsEnabled`.
- Current cost shape:
  - One allocation per adjacency query from `ToArray()` even when the caller only needs to know whether the count exceeds one.
  - Full traversal of adjacent transitions even after the second enabled transition is discovered.
  - Repeated iterator/delegate churn through LINQ for the hottest conflict-detection path named in the remediation plan.
- Planned remediation:
  - Introduce a non-allocating early-exit path for conflict checks that counts enabled adjacent transitions only up to two.
  - Preserve the existing public `GetEnabledTransitionsAdjacentToPlace` behavior if still needed externally, but stop routing conflict checks through a materializing helper.
  - Keep `IsEnabled` parity intact while removing the `ToArray()` dependency from this path.

### 2. Graph enablement and firing paths in `GraphPetriNet`

- Current code:
  - `NonInhibitorsIntoTransition(int transitionId)` returns `GetInArcs(transitionId).Where(...).Select(...)`.
  - `AllEnabledTransitions(Marking m)` is a LINQ query over all transitions.
  - `Fire(Marking m)` uses `GetInArcs(transitionId).Where(static x => x.IsInhibitor == false)` in the inner firing loop.
  - `GetPlaceOutArcs(int placeId)` returns `PlaceOutArcs[placeId].Select(x => x.Target)` when arcs exist.
- Current cost shape:
  - Iterator chains are constructed on each enablement and firing pass even though the backing storage is already a `List<InArc>` or `List<OutArc>`.
  - Base enablement currently combines graph-specific iterator chains with base-class `All(...)` calls and repeated `GetWeight(...)` lookups.
  - Conflict and firing preparation can repeatedly scan transition and arc data through LINQ instead of a direct list loop.
- Planned remediation:
  - Replace hot-path LINQ over `List<InArc>` and `List<OutArc>` with direct loops in the enablement and firing paths.
  - Favor `TryGetValue` and local list references in the hot loops where the current code uses `ContainsKey` plus indexer or returns empty arrays.
  - Keep constructor-time `ToList()`/`ToDictionary()` cloning unchanged unless a touched path proves it is part of the runtime hotspot; R2 is about steady-state execution, not build-time graph normalization.

### 3. Matrix enablement and firing paths in `MatrixPetriNet`

- Current code:
  - `AllInhibitorsAreFromEmptyPlaces` and `AllInArcPlacesHaveMoreTokensThanTheArcWeight` use enumerable helpers and `All(...)`.
  - `GetEnabledTransitions(Marking m)` yields enabled transitions one by one.
  - `Fire(Marking m)` clones the marking, then loops `place x firingPlan.TransitionIds` and calls `GetInputDelta(placeId, transitionId)` inside the inner loop.
- Current cost shape:
  - Enablement performs repeated enumerable-based scans and method dispatch in a path already named critical by the remediation document.
  - The fire loop remains dense and cache-unfriendly; R2 cannot solve the sparse-kernel problem, but it can still reduce avoidable helper-call and iterator overhead within the current dense algorithm.
  - `new Marking(m)` is a measurable contributor to bytes/op in every fire call even though AM-2-level semantic changes are out of scope.
- Planned remediation:
  - Collapse matrix enablement checks into explicit loops over the matrix rows for the transition under test, eliminating the layered enumerable path in the hot case.
  - In `Fire`, use local references and direct conditional arithmetic in the inner loop instead of calling a helper per `(place, transition)` pair when that helper only branches on inhibitor status.
  - Explicitly defer any sparse traversal redesign, adjacency precomputation, or in-place marking update mode to later remediation items.

### 4. `Marking` materialization paths

- Current code:
  - `Marking(int[] vec)` clones with `vec.ToArray()`.
  - `Marking(Marking m)` clones with `Array.Copy(...)` and is used by `GraphPetriNet.Fire` and `MatrixPetriNet.Fire`.
- Current cost shape:
  - Copying is unavoidable under current semantics, but it is part of the measured allocation story for fire benchmarks.
  - The remediation plan's AM-2 notes that deeper change here would require an in-place or update-buffer mode, which is outside this feature.
- Planned remediation:
  - Do not redesign marking ownership or mutability in R2.
  - Benchmark and profile marking copy cost so the plan can distinguish between allocation reductions achieved by iterator/materialization cleanup and the residual floor that belongs to a later AM-2 feature.

## Implementation Strategy

### Slice A: Remove base conflict-path materialization

- Replace the current `ToArray()` plus `Count() > 1` pipeline with an early-exit helper that returns as soon as a second enabled adjacent transition is found.
- Keep conflict semantics identical for empty places, single enabled transitions, and high-fan-out places.
- Validate with `PetriNetBaseProperties` parity over generated nets and markings.

### Slice B: Replace graph LINQ in enablement and fire loops

- Introduce direct loops over `InArcs` and `OutArcs` in the graph hot paths used by enablement, firing-plan creation support, and `Fire`.
- Remove iterator-heavy `Where`/`Select` chains from runtime execution paths while keeping public shapes stable.
- Validate with graph-vs-matrix parity properties and dispatch-order checks.

### Slice C: Tighten matrix hot loops without crossing into R5

- Rewrite matrix enablement checks and fire inner loops to use explicit loops and local matrix references.
- Keep the current dense traversal model and token-update semantics exactly intact.
- Measure whether this reduces bytes/op and instruction overhead even before any later sparse-kernel redesign.

### Slice D: Extend the benchmark and profiling harness to cover R2

- The current `perf/core.benchmarks` project only benchmarks `GetNextTransitionToFire`, which primarily validates R1.
- Add benchmark cases for:
  - `PetriNetBase`-style conflict detection on high-fan-out places.
  - Graph fire on representative enabled-plan scenarios.
  - Matrix fire on representative dense enabled-plan scenarios.
  - Optional targeted enablement benchmarks where they isolate a changed hotspot more clearly than whole-fire benchmarks.
- Add scenario builders that model the shapes named in the remediation plan: medium and large transition counts, varying arc density, and representative conflict/non-conflict cases.

## Validation Strategy

### Property-based verification

- Extend `PetriNetBaseProperties` to cover unchanged conflict detection behavior and unchanged adjacent-enabled semantics after the early-exit rewrite.
- Extend graph and matrix parity properties so that firing results and transition-function invocation order remain identical after loop rewrites.
- Keep properties broad: random nets, mixed inhibitor/non-inhibitor inputs, empty enabled sets, and conflict-heavy fan-out cases.

### Benchmark validation

- Use `BenchmarkDotNet` with `MemoryDiagnoser` in the committed `perf/core.benchmarks` project.
- Baseline and compare:
  - graph conflict check throughput and allocations
  - graph fire throughput and allocations
  - matrix fire throughput and allocations
  - any focused enablement benchmark introduced to isolate a hotspot
- The acceptance target for the feature remains benchmarked bytes/op reduction of at least 30% in the representative firing path, with hotspot-specific evidence attached for each changed area.

### Profiling validation

- Capture allocation call trees before and after each hotspot slice using the repository's intended PerfView or equivalent allocation-trace workflow.
- Require the named source hotspot to visibly shrink in the call tree, not just the aggregate total.
- Use profiling output to separate residual `Marking` copy cost from the removable iterator/materialization overhead addressed by R2.

## Non-Goals and Explicit Deferrals

- Do not introduce pooled buffers or `ArrayPool` ownership in this feature; that is a later R6-level change and would complicate lifetime management too early.
- Do not redesign matrix traversal around sparse adjacency paths; that belongs to R5.
- Do not add reverse name-to-index maps; that belongs to R3.
- Do not redesign marking ownership or introduce in-place update semantics; that belongs to AM-2 and a separate feature.
- Do not attempt repo-wide LINQ removal outside the measured hotspots listed above.

## Risks and Mitigations

| Risk | Why it matters here | Mitigation |
|------|---------------------|------------|
| Conflict-path rewrite changes short-circuit behavior | `PlaceIsConflicted` currently observes count semantics indirectly through materialization | Prove parity with generated high-fan-out and low-fan-out properties before accepting the change |
| Graph loop rewrites subtly change handler or token-update order | `Fire` mixes data updates and dispatch-sensitive behavior | Keep parity properties for resulting markings and transition-function invocation order across equivalent graph and matrix nets |
| Matrix loop cleanup produces little win because marking copy still dominates allocations | AM-2 remains out of scope, so residual allocation floor may stay high | Separate hotspot benchmarks from whole-fire benchmarks and use profiling to attribute the remaining cost explicitly |
| Existing benchmark harness is too R1-specific to validate R2 | Current committed benchmarks only exercise selection | Extend the harness before judging the implementation, and make benchmark work part of the feature rather than optional cleanup |

## Complexity Tracking

No constitution violations are required for this feature.
