# Research: Max-Scan Transition Selection

## Decision 1: Use a single-pass max-scan with first-seen tie preservation

- Decision: Replace LINQ ordering in `GraphPetriNet.GetNextTransitionToFire` and `MatrixPetriNet.GetNextTransitionToFire` with an explicit single-pass scan over enabled transitions. Update the current best transition only when a strictly higher priority is seen.
- Rationale: The current implementation sorts enabled transitions to pick one winner. A max-scan removes ordering overhead and allocations while preserving observable behavior. By only replacing the winner on strictly greater priority, the first enabled transition encountered remains the winner on ties, matching current stable enumeration behavior.
- Alternatives considered: Keep LINQ ordering and accept hot-path overhead; use `MaxBy`-style helpers that still obscure tie behavior and may add dependency/runtime variability; pre-sort transitions globally, which would not respect enabled-set filtering cleanly.

## Decision 2: Keep the feature scoped to selection logic and its direct call sites

- Decision: Change only the transition-selection call sites identified in the remediation plan: `GraphPetriNet.GetNextTransitionToFire` and `MatrixPetriNet.GetNextTransitionToFire`.
- Rationale: The spec and remediation plan both classify R1 as a low-risk hot-path optimization. Broader loop rewrites, enablement caching, or firing-path materialization removal belong to later remediation items (R2 and beyond) and would increase change risk without being necessary to satisfy R1.
- Alternatives considered: Fold in `ToList`/`ToArray` removal from fire paths now; combine this work with enablement refactors; redesign matrix firing traversal. These were rejected because they belong to separate recommendations with larger semantic surface area.

## Decision 3: Validate behavioral parity with focused tests and reproducible microbenchmarks

- Decision: Add FsCheck/FsCheck.Xunit properties for highest-priority selection, equal-priority tie behavior, empty enabled sets, and default-zero priority behavior, and add committed BenchmarkDotNet microbenchmarks for both graph and matrix selection paths.
- Rationale: The constitution requires property-based TDD and rejects example-style tests disguised as properties. This feature's correctness hinges on invariants over many enabled-transition combinations, which FsCheck can explore directly. Committed benchmarks satisfy the clarified requirement that performance evidence is reproducible from source.
- Alternatives considered: Rely only on the existing test suite; write example-based unit tests for a few fixed nets; use ad hoc local measurements; benchmark only one execution path. These approaches were rejected because they violate the constitution or leave key acceptance criteria under-specified.

## Decision 4: Add a dedicated benchmark project under `perf/`

- Decision: Introduce a new `perf/core.benchmarks` project referencing `src/core` and using BenchmarkDotNet with memory diagnostics.
- Rationale: Benchmarks should be isolated from the unit-test project so they can use Release settings, BenchmarkDotNet configuration, and performance-focused dependencies without affecting test execution. A dedicated `perf/` root also makes the deliverable explicit for future remediation items.
- Alternatives considered: Put benchmarks inside `test/core.tests`; use a single ad hoc console app; omit committed benchmarks. These were rejected because they either blur test/perf concerns or fail the clarified spec requirement.

## Decision 5: Express selection contracts with idiomatic C# rather than new Code Contracts usage

- Decision: Preserve and clarify method contracts for `GetNextTransitionToFire`, `CreateFiringPlan`, and `Fire` using existing signatures, nullable return types, guard clauses where needed, and behavioral documentation/assertions rather than adding new Code Contracts usage.
- Rationale: The constitution requires explicit contracts but forbids expanding the deprecated Code Contracts library. This feature is contract-sensitive because null return behavior, tie behavior, and no-mutation expectations are central to correctness.
- Alternatives considered: Leave the contract implicit in the implementation; add more `System.Diagnostics.Contracts` usage; widen API signatures to expose more state. These were rejected because they either weaken correctness guarantees or violate the constitution.
