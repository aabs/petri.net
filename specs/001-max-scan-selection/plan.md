# Implementation Plan: Max-Scan Transition Selection

**Branch**: `001-max-scan-selection` | **Date**: 2026-04-12 | **Spec**: `/Users/aabs/dev/aabs/active/computational-models/petri.net/specs/001-max-scan-selection/spec.md`
**Input**: Feature specification from `/specs/001-max-scan-selection/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Introduce a shared firing plan abstraction used by both `GraphPetriNet` and `MatrixPetriNet`, then extract firing-plan consumption into a single dispatcher that turns a plan into transition actions through the handlers attached to the selected transitions. Align both models on that canonical step semantics and shared dispatch path first, then replace LINQ-based priority ordering inside that abstraction with a single-pass max-scan that preserves exact semantics, null behavior, and deterministic tie handling. Drive the change with FsCheck/FsCheck.Xunit properties written first, express method contracts with idiomatic C# guard and nullability patterns rather than new Code Contracts usage, and validate the performance goal with committed BenchmarkDotNet microbenchmarks for both graph and matrix paths.

## Technical Context

**Language/Version**: C# 14 on .NET 10  
**Primary Dependencies**: `MathNet.Numerics` in `src/core`; `xUnit`, `FsCheck`, and `FsCheck.Xunit` in `test/core.tests`; `BenchmarkDotNet` in the new benchmark project  
**Storage**: N/A  
**Testing**: `dotnet test` with FsCheck.Xunit property-based tests under red-green-refactor discipline; BenchmarkDotNet for committed microbenchmarks  
**Target Platform**: .NET 10 on macOS, Linux, and Windows  
**Project Type**: Multi-project .NET library repository with parser and test projects  
**Performance Goals**: Preserve exact behavior first, align graph and matrix step semantics through a shared firing plan abstraction and dispatcher, then achieve at least 20% lower CPU per `GetNextTransitionToFire` call on representative large nets with no additional steady-state allocations per call  
**Constraints**: No public API breaks; shared firing plan semantics and dispatch behavior must be canonical for both models; firing-plan actioning must live in one dispatcher code path rather than duplicated model-specific execution logic; properties must cover behavioral classes rather than disguised examples; new code must not add deprecated Code Contracts usage  
**Scale/Scope**: Code changes are limited to shared firing-plan, dispatch, and transition-selection paths in `src/core`, property suites in `test/core.tests`, and a committed benchmark project under `perf/` covering graph and matrix nets with 100+ transitions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- C# 14 on .NET 10: PASS. All planned implementation, tests, and benchmarks remain within the mandated runtime and language version.
- Correctness before performance: PASS. The feature first aligns graph and matrix execution through a shared firing plan abstraction and shared dispatcher, then treats benchmark wins as secondary validation after parity is proven.
- Property-based TDD: PASS. The design requires FsCheck/FsCheck.Xunit properties for tie behavior, highest-priority selection, sparse priority maps, and empty enabled sets before production edits.
- Behavioral properties, not disguised examples: PASS. Planned properties describe invariants over generated nets and markings rather than single hard-coded examples.
- Idiomatic contracts, no new Code Contracts: PASS. The change introduces shared step-semantics and dispatch infrastructure without adding deprecated Code Contracts usage and will rely on guard clauses, nullable expectations, and assertions/documentation where needed.

Post-design re-check: PASS. Phase 1 artifacts keep the implementation scoped, preserve semantics, and encode both property-first testing and idiomatic contract expression.

## Project Structure

### Documentation (this feature)

```text
specs/001-max-scan-selection/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── transition-selection-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── core/
│   ├── FiringPlan.cs
│   ├── FiringPlanDispatcher.cs
│   ├── FiringPlanner.cs
│   ├── GraphPetriNet.cs
│   ├── MatrixPetriNet.cs
│   ├── PetriNetBase.cs
│   └── petrinets2.core.csproj
└── arclang/
  └── arclang.csproj

test/
└── core.tests/
  ├── core.tests.csproj
  ├── FiringPlanDispatcherProperties.cs
  ├── FiringPlanProperties.cs
  ├── GraphPetriNetProperties.cs
  └── MatrixPetriNetProperties.cs

perf/
└── core.benchmarks/
  ├── core.benchmarks.csproj
  └── TransitionSelectionBenchmarks.cs
```

**Structure Decision**: Keep the existing repository layout. Implement the shared firing plan abstraction, a shared `FiringPlanDispatcher` that consumes plans and invokes transition handlers, and the max-scan production change in `src/core`. Add or extend FsCheck.Xunit property files in `test/core.tests`, and add a dedicated `perf/core.benchmarks` project for reproducible BenchmarkDotNet validation. The shared firing plan and dispatcher layers are foundational so later performance changes optimize one canonical execution-and-dispatch path instead of two diverging model implementations.

## Complexity Tracking

No constitution violations are required for this feature.
