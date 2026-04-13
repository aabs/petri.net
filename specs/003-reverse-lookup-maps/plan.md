# Implementation Plan: Reverse Lookup Maps for Names to Indices

**Branch**: `003-reverse-lookup-maps` | **Date**: 2026-04-13 | **Spec**: `/specs/003-reverse-lookup-maps/spec.md`
**Input**: Feature specification from `/specs/003-reverse-lookup-maps/spec.md`

## Summary

Replace linear name-to-index scans in `CreatePetriNet` and `PnmlModelLoader` with constant-time dictionary lookups by introducing builder-owned reverse maps (`name -> index`) and PNML place-id lookup maps. Preserve public API signatures and externally observable behavior while modernizing touched code toward idiomatic guard clauses and adding property-based invariants plus benchmark coverage for construction and PNML load paths.

## Technical Context

**Language/Version**: C# 14 on .NET 10 (`net10.0`)  
**Primary Dependencies**: `MathNet.Numerics` (core), `FsCheck` + `FsCheck.Xunit` + `xUnit` (tests), `BenchmarkDotNet` (perf)  
**Storage**: N/A (in-memory dictionaries and PNML file input)  
**Testing**: Property-based tests with FsCheck/FsCheck.Xunit in `test/core.tests`  
**Target Platform**: .NET 10 on Linux/Windows/macOS
**Project Type**: Monolithic multi-project .NET library + parser + benchmarks  
**Performance Goals**: Remove O(n) value scans from builder lookups and PNML place-id resolution; meet SC-002/SC-003 >= 25% improvement targets; avoid allocation regressions (SC-006)  
**Constraints**: Preserve behavior parity across graph/matrix construction, keep public signatures stable, avoid introducing new Code Contracts usage, correctness before optimization  
**Scale/Scope**: Large nets up to at least 1,000 places / 5,000 transitions / 10,000 arcs in construction benchmarks and >500 places/arcs in PNML benchmark scenarios

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Gate

- Pass: Plan targets idiomatic C# 14 on .NET 10 and keeps existing runtime/test stack.
- Pass: Correctness and contract behavior are defined before optimization claims (missing-name failure mode, map consistency invariants, API compatibility constraints).
- Pass: Test strategy is property-first with FsCheck/FsCheck.Xunit; implementation tasks must follow red-green-refactor.
- Pass: Planned properties are invariant and sequence based (forward/reverse map equivalence, duplicate stability), not canned examples.
- Pass: Contract expression will use idiomatic guards/exceptions; no new `System.Diagnostics.Contracts` usage will be added.

### Post-Phase 1 Re-Check

- Pass: `data-model.md` defines map invariants and mutation rules.
- Pass: `contracts/reverse-lookup-contract.md` defines inputs/outputs/failure modes for builder and PNML surfaces.
- Pass: `quickstart.md` includes property-first and benchmark validation flow before claiming performance wins.
- Pass: No constitution violations identified.

## Project Structure

### Documentation (this feature)

```text
specs/003-reverse-lookup-maps/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── reverse-lookup-contract.md
└── tasks.md                # created by /speckit.tasks
```

### Source Code (repository root)

```text
src/
├── core/
│   ├── builders/
│   │   ├── CreatePetriNet.cs            # primary reverse-map changes
│   │   └── PetriNetConnectionBuilder.cs # indirect caller of AddInArc/AddOutArc
│   ├── PnmlModelLoader.cs               # PNML place-id lookup updates
│   ├── GraphPetriNet.cs
│   └── MatrixPetriNet.cs
├── arclang/
└── cli/

test/
└── core.tests/
    ├── GraphPetriNetProperties.cs
    ├── MatrixPetriNetProperties.cs
    └── [new reverse-map property test file]

perf/
└── core.benchmarks/
    ├── Program.cs
    ├── HotPathAllocationBenchmarks.cs
    ├── TransitionSelectionBenchmarks.cs
    └── [new construction/pnml benchmark files]
```

**Structure Decision**: Keep the existing monolithic multi-project layout and implement the feature by touching only `src/core`, adding property tests in `test/core.tests`, and extending benchmark coverage in `perf/core.benchmarks`.

## Phase 0 Research Plan

1. Confirm reverse-map ownership and synchronization strategy in `CreatePetriNet`.
2. Confirm modern, explicit failure semantics for missing names/place IDs.
3. Confirm PNML load and marking resolution lookup design without value scans.
4. Confirm property-based invariant strategy for forward/reverse consistency.
5. Confirm benchmark strategy and diagnosers for throughput + allocations.

Output artifact: `research.md`.

## Phase 1 Design Plan

1. Define entities, fields, invariants, and state transitions for forward/reverse maps and PNML lookup maps in `data-model.md`.
2. Define interface contracts for builder lookups, insertion semantics, and PNML resolution/failure modes in `contracts/reverse-lookup-contract.md`.
3. Capture implementation and validation workflow (property-first, then code, then benchmark evidence) in `quickstart.md`.
4. Run `.specify/scripts/bash/update-agent-context.sh copilot` to sync agent context with the plan.

Output artifacts: `data-model.md`, `contracts/reverse-lookup-contract.md`, `quickstart.md`, updated agent context file.

## Phase 2 Preview (for /speckit.tasks)

- Write failing FsCheck properties for reverse-map invariants and behavior parity.
- Implement `CreatePetriNet` reverse maps and O(1) duplicate/name resolution with guard-based failures.
- Update PNML loader lookup path for `Load` and `LoadMarkings`.
- Add/extend deterministic benchmarks for construction and PNML load.
- Run full tests and benchmark validation gates.

## Complexity Tracking

No constitution violations were required for this plan.
