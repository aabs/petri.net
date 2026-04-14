# Implementation Plan: Sparse Matrix Fire Kernel Redesign

**Branch**: `005-redesign-sparse-matrix-kernel` | **Date**: 2026-04-14 | **Spec**: `/specs/004-redesign-sparse-matrix-kernel/spec.md`
**Input**: Feature specification from `/specs/004-redesign-sparse-matrix-kernel/spec.md`

## Summary

Rebuild `MatrixPetriNet` internals around sparse connectivity-aware execution so fire and state-equation computation inspect only existing place/transition links rather than full-grid traversal. Preserve external API signatures and observable behavior (post-fire marking, exact state-equation result values, side-effect order, enablement/conflict semantics, determinism), allow any internal sparse representation strategy, and validate against explicit performance guardrails for both fire and state-equation workloads.

## Technical Context

**Language/Version**: C# 14 on .NET 10 (`net10.0`)  
**Primary Dependencies**: `MathNet.Numerics` (current matrix engine; sparse-capable), `FsCheck` + `FsCheck.Xunit` + `xUnit` (tests), `BenchmarkDotNet` (perf)  
**Storage**: N/A (in-memory markings and incidence structures)  
**Testing**: Property-based tests with FsCheck/FsCheck.Xunit plus existing regression suite  
**Target Platform**: .NET 10 on Linux/Windows/macOS  
**Project Type**: Monolithic multi-project .NET library repository (core + parser + benchmarks)  
**Performance Goals**: >=2.0x sparse fire throughput, >=2.0x sparse state-equation throughput, <=5% dense-profile regression for both workloads, sparse allocation non-increasing or justified  
**Constraints**: Preserve externally observable semantics, preserve transition side-effect invocation order, keep `MatrixPetriNet` public interface unchanged, no new Code Contracts usage, no SIMD/pooling redesign in this feature, runtime fallback to baseline is not required post-migration  
**Scale/Scope**: Internal matrix representation and execution changes in `src/core/MatrixPetriNet.cs` and related internal helpers, plus parity tests and sparse/dense benchmark scenarios for fire and state-equation workloads

**Clarification Alignment**: FR-014 requires benchmark acceptance gating by density bands (low/medium/high) without fixed place-transition tuple sizes; FR-015 requires exact state-equation parity value equality.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Gate

- Pass: Plan targets idiomatic C# 14 on .NET 10 and current repository stack.
- Pass: Correctness and contract-sensitive behavior are explicitly defined before optimization claims.
- Pass: Test strategy is property-based with FsCheck/FsCheck.Xunit and red-green-refactor ordering.
- Pass: Planned tests are invariants/parity properties over generated model families, not canned examples.
- Pass: Contract expression uses idiomatic C# guards/assertions and does not introduce new `System.Diagnostics.Contracts` usage.

### Post-Phase 1 Re-Check

- Pass: `data-model.md` defines sparse execution entities and weighted incidence invariants that preserve baseline semantics for fire and state equation.
- Pass: `contracts/matrix-fire-kernel-contract.md` defines behavior, compatibility, and failure boundaries for dense-to-sparse representation migration.
- Pass: `quickstart.md` enforces property-first validation and benchmark evidence workflow for both workloads.
- Pass: No constitution violations identified.

## Project Structure

### Documentation (this feature)

```text
specs/004-redesign-sparse-matrix-kernel/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── matrix-fire-kernel-contract.md
└── tasks.md                # created by /speckit.tasks
```

### Source Code (repository root)

```text
src/
├── core/
│   ├── MatrixPetriNet.cs
│   ├── FiringPlanner.cs
│   ├── PetriNetBase.cs
│   ├── TransitionSelection.cs
│   └── builders/
├── arclang/
└── cli/

test/
└── core.tests/
    ├── MatrixPetriNetProperties.cs
    ├── GraphPetriNetProperties.cs
    └── [new matrix fire parity/determinism property files]

perf/
└── core.benchmarks/
    ├── Program.cs
    ├── HotPathAllocationBenchmarks.cs
    ├── TransitionSelectionBenchmarks.cs
    ├── TransitionSelectionBenchmarkScenarios.cs
    └── [new or extended matrix fire sparse/dense benchmark scenarios]
```

**Structure Decision**: Keep existing repository layout; implement representation migration and execution-path changes in `src/core`, add property-based parity/determinism/state-equation coverage in `test/core.tests`, and extend benchmark scenarios in `perf/core.benchmarks` for sparse/dense guardrails.

## Phase 0 Research Results

1. Chosen representation model: sparse connectivity-aware internal execution with implementation-agnostic sparse storage strategy.
2. Chosen parity strategy: differential oracle comparing baseline dense and sparse-backed paths for marking/side-effects/enablement/conflicts/determinism, with exact state-equation result-value equality.
3. Chosen migration strategy: external API unchanged, internal representation switched without runtime fallback requirement.
4. Chosen performance validation: density-sweep BenchmarkDotNet scenarios (low/medium/high density bands, no fixed tuple-size gate) with MemoryDiagnoser and explicit fire/state-equation measurements.

Output artifact: `research.md`.

## Phase 1 Design Outputs

1. `data-model.md` defining sparse execution entities, weighted incidence invariants, and state transitions.
2. `contracts/matrix-fire-kernel-contract.md` defining contract-sensitive behavior, compatibility boundaries, and migration constraints.
3. `quickstart.md` defining sparse migration workflow, property-first workflow, and benchmark validation gates.
4. Agent context updated via `.specify/scripts/bash/update-agent-context.sh copilot`.

Output artifacts: `data-model.md`, `contracts/matrix-fire-kernel-contract.md`, `quickstart.md`, updated agent context.

## Phase 2 Preview (for /speckit.tasks)

- Add failing FsCheck properties for sparse non-zero traversal constraints and baseline parity invariants across fire and state equation.
- Implement sparse-backed internal representation and fire/state-equation execution path while preserving baseline behavior.
- Add determinism properties and parity oracle harness, including exact state-equation result-value equality checks.
- Extend sparse/dense benchmark scenarios and state-equation measurements using density-band acceptance gates, then compare against SC-002 through SC-007 thresholds.
- Run full test + benchmark validation and record evidence for graduation decision.

## Requirement Mapping

- FR-014 planning implication: define benchmark acceptance by density bands only (low/medium/high), avoiding fixed tuple-size gating.
- FR-015 planning implication: require exact state-equation output-value equality in differential properties and acceptance evidence.

## Complexity Tracking

No constitution violations were required for this plan.
