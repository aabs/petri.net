# Implementation Plan: Scratch Buffer Pooling

**Branch**: `005-scratch-buffer-pooling` | **Date**: 2026-04-14 | **Spec**: `/specs/005-scratch-buffer-pooling/spec.md`
**Input**: Feature specification from `/specs/005-scratch-buffer-pooling/spec.md`

## Summary

Introduce pooled scratch-buffer reuse for steady-state planning and firing hotspots using thread-local capped pools plus shared fallback acquisition. Preserve existing external behavior and API shape while enforcing explicit lease ownership, bounded fallback retries (5 retries with exponential backoff), and failure atomicity. Validate with property-based parity/determinism tests and benchmark evidence for allocation reduction and throughput guardrails.

## Technical Context

**Language/Version**: C# 14 on .NET 10 (`net10.0`)  
**Primary Dependencies**: `MathNet.Numerics` (core), `FsCheck` + `FsCheck.Xunit` + `xUnit` (tests), `BenchmarkDotNet` (perf)  
**Storage**: N/A (in-memory markings, transition-selection scratch, and fire-delta scratch)  
**Testing**: Property-based tests with FsCheck/FsCheck.Xunit plus existing regression suite  
**Target Platform**: .NET 10 on Linux/Windows/macOS  
**Project Type**: Monolithic multi-project .NET library repository (core + parser + benchmarks)  
**Performance Goals**: >=20% allocation-rate reduction on targeted steady-state planning/fire workloads; <=5% throughput regression threshold versus baseline; determinism and parity remain 100% for defined properties  
**Constraints**: Preserve externally observable behavior; keep public API unchanged; enforce explicit lease ownership and release semantics; use idiomatic C# contracts only; apply pooling only to scoped steady-state hotspots  
**Scale/Scope**: Changes focused in `src/core` planning/fire internals and related tests/benchmarks in `test/core.tests` and `perf/core.benchmarks`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Gate

- Pass: Plan targets idiomatic C# 14 on .NET 10 and repository-approved stack.
- Pass: Correctness-preserving behavior and explicit failure contracts are defined before performance implementation.
- Pass: Validation strategy is property-based with FsCheck/FsCheck.Xunit under red-green-refactor discipline.
- Pass: Planned tests focus on behavioral properties (parity, determinism, ownership invariants), not canned examples.
- Pass: Contract expression uses guard clauses and explicit domain invariants; no new Code Contracts usage.

### Post-Phase 1 Re-Check

- Pass: `data-model.md` defines lease lifecycle, pool topology, bounded-cap behavior, and acquisition retry state transitions.
- Pass: `contracts/scratch-buffer-pooling-contract.md` captures observable behavior, acquisition failures, and compatibility boundaries.
- Pass: `quickstart.md` enforces property-first validation and benchmark evidence workflow.
- Pass: No constitution violations identified.

## Project Structure

### Documentation (this feature)

```text
specs/005-scratch-buffer-pooling/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── scratch-buffer-pooling-contract.md
└── tasks.md                # created by /speckit.tasks
```

### Source Code (repository root)

```text
src/
├── core/
│   ├── GraphPetriNet.cs
│   ├── MatrixPetriNet.cs
│   ├── FiringPlanner.cs
│   ├── TransitionSelection.cs
│   └── builders/
├── arclang/
└── cli/

test/
└── core.tests/
    ├── [existing property suites]
    └── [new pooling parity/ownership/determinism properties]

perf/
└── core.benchmarks/
    ├── HotPathAllocationBenchmarks.cs
    ├── TransitionSelectionBenchmarks.cs
    ├── TransitionSelectionBenchmarkScenarios.cs
    └── [new or extended pooling-focused scenarios]
```

**Structure Decision**: Keep existing repository layout. Implement pooling internals in `src/core`, add new property-based validation in `test/core.tests`, and extend benchmark scenarios in `perf/core.benchmarks` for targeted allocation and throughput evidence.

## Phase 0 Research Results

1. Use thread-local pools with shared fallback instead of a single globally locked pool.
2. Bound per-thread retained capacity via internal configurable cap and overflow to shared fallback.
3. Use bounded acquisition retries with exponential backoff (5 retries) and explicit failure on exhaustion.
4. Enforce failure atomicity for acquisition exhaustion (no partial planning or marking state mutation).
5. Keep tuning and retry policy internal/non-public to preserve API compatibility.
6. Validate via property-based parity/determinism/ownership properties and benchmark allocation + throughput gates.

Output artifact: `research.md`.

## Phase 1 Design Outputs

1. `data-model.md` defining lease lifecycle, pool topology, cap/overflow behavior, and retry state transitions.
2. `contracts/scratch-buffer-pooling-contract.md` defining acquisition/release semantics, failure modes, and compatibility contract.
3. `quickstart.md` defining property-first implementation and benchmark validation workflow.
4. Agent context update via `.specify/scripts/bash/update-agent-context.sh copilot`.

Output artifacts: `data-model.md`, `contracts/scratch-buffer-pooling-contract.md`, `quickstart.md`, updated agent context.

## Phase 2 Preview (for /speckit.tasks)

- Add failing FsCheck properties for parity, determinism, lease ownership, double-return prevention, and failure atomicity.
- Implement planning-path scratch pooling with explicit lease boundaries.
- Implement firing-path delta scratch pooling with explicit lease boundaries.
- Implement per-thread cap and shared fallback acquisition path.
- Implement internal 5-retry exponential-backoff strategy for fallback acquisition.
- Add explicit acquisition-exhaustion exception behavior and atomicity guards.
- Extend/author benchmarks for steady-state planning/fire allocation and throughput comparisons.
- Run full test and benchmark gates and document SC-001 to SC-005 evidence.

## Requirement Mapping

- FR-005/FR-006 planning implication: define ownership model and lease lifecycle to avoid cross-call contamination.
- FR-009/FR-010 planning implication: specify bounded retry behavior and no-partial-mutation failure semantics.
- FR-011/FR-012 planning implication: define internal cap configuration and non-public tuning boundaries.

## Implementation Traceability Update (2026-04-14)

- FR-001/FR-002: Implemented pooled planning and firing scratch paths in `src/core/FiringPlanner.cs`, `src/core/GraphPetriNet.cs`, and `src/core/MatrixPetriNet.cs`; parity properties in `test/core.tests/ScratchBufferPoolingParityProperties.cs`.
- FR-003/FR-004: Verified behavioral parity and determinism via `test/core.tests/ScratchBufferPoolingParityProperties.cs` and `test/core.tests/ScratchBufferDeterminismProperties.cs`.
- FR-005/FR-006: Implemented thread-local ownership, lease lifecycle, and exactly-once release guards in `src/core/ScratchBufferPooling.cs`; ownership properties in `test/core.tests/ScratchBufferOwnershipProperties.cs`.
- FR-009/FR-010: Implemented 5-retry exponential backoff and acquisition-exhaustion atomicity in `src/core/ScratchBufferPooling.cs` with validation in `test/core.tests/ScratchBufferFailureModeProperties.cs`.
- FR-011/FR-012: Implemented configurable cap defaults and bounds in `src/core/ScratchBufferPooling.cs`; cap-bound validation in `test/core.tests/ScratchBufferFailureModeProperties.cs`.

## API Compatibility Verification (2026-04-14)

- Verified no public API signature changes were introduced in `src/core/GraphPetriNet.cs` and `src/core/MatrixPetriNet.cs`.
- Scratch-buffer pooling additions are internal (`src/core/ScratchBufferPooling.cs`) and exposed to tests/benchmarks via `InternalsVisibleTo` only.

## Complexity Tracking

No constitution violations were required for this plan.
