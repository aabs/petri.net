# Implementation Plan: Max-Scan Transition Selection

**Branch**: `001-max-scan-selection` | **Date**: 2026-04-12 | **Spec**: `/Users/aabs/dev/aabs/active/computational-models/petri.net/specs/001-max-scan-selection/spec.md`
**Input**: Feature specification from `/specs/001-max-scan-selection/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Replace LINQ-based priority ordering in `GraphPetriNet.GetNextTransitionToFire` and `MatrixPetriNet.GetNextTransitionToFire` with a single-pass max-scan that preserves exact semantics, null behavior, and deterministic tie handling. Drive the change with FsCheck/FsCheck.Xunit properties written first, express method contracts with idiomatic C# guard and nullability patterns rather than new Code Contracts usage, and validate the performance goal with committed BenchmarkDotNet microbenchmarks for both graph and matrix paths.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: C# 14 on .NET 10  
**Primary Dependencies**: `MathNet.Numerics` in `src/core`; `xUnit`, `FsCheck`, and `FsCheck.Xunit` in `test/core.tests`; `BenchmarkDotNet` in the new benchmark project  
**Storage**: N/A  
**Testing**: `dotnet test` with FsCheck.Xunit property-based tests under red-green-refactor discipline; BenchmarkDotNet for committed microbenchmarks  
**Target Platform**: .NET 10 on macOS, Linux, and Windows  
**Project Type**: Multi-project .NET library repository with parser and test projects  
**Performance Goals**: Preserve exact behavior first, then achieve at least 20% lower CPU per `GetNextTransitionToFire` call on representative large nets with no additional steady-state allocations per call  
**Constraints**: No public API breaks; no conflict-semantics change; no transition-priority model change; properties must cover behavioral classes rather than disguised examples; new code must not add deprecated Code Contracts usage  
**Scale/Scope**: Code changes are limited to transition selection paths in `src/core`, property suites in `test/core.tests`, and a committed benchmark project under `perf/` covering graph and matrix nets with 100+ transitions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- C# 14 on .NET 10: PASS. All planned implementation, tests, and benchmarks remain within the mandated runtime and language version.
- Correctness before performance: PASS. The feature preserves observable selection semantics first and treats benchmark wins as secondary validation after parity is proven.
- Property-based TDD: PASS. The design requires FsCheck/FsCheck.Xunit properties for tie behavior, highest-priority selection, sparse priority maps, and empty enabled sets before production edits.
- Behavioral properties, not disguised examples: PASS. Planned properties describe invariants over generated nets and markings rather than single hard-coded examples.
- Idiomatic contracts, no new Code Contracts: PASS. The change is limited to existing APIs and will use guard clauses, nullable expectations, and assertions/documentation where needed instead of introducing new Code Contracts usage.

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
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real paths (e.g., apps/admin, packages/something). The delivered plan must
  not include Option labels.
-->

```text
src/
├── core/
│   ├── GraphPetriNet.cs
│   ├── MatrixPetriNet.cs
│   └── petrinets2.core.csproj
└── arclang/
  └── arclang.csproj

test/
└── core.tests/
  ├── core.tests.csproj
  ├── GraphPetriNetProperties.cs
  └── MatrixPetriNetProperties.cs

perf/
└── core.benchmarks/
  ├── core.benchmarks.csproj
  └── TransitionSelectionBenchmarks.cs
```

**Structure Decision**: Keep the existing repository layout. Implement the max-scan production change in `src/core`, add or extend FsCheck.Xunit property files in `test/core.tests`, and add a dedicated `perf/core.benchmarks` project for reproducible BenchmarkDotNet validation. No new service or storage layers are needed because this feature is purely in-process library behavior.

## Complexity Tracking

No constitution violations are required for this feature.
