# Phase 0 Research: Reverse Lookup Maps

## Decision 1: Builder-owned reverse maps for names to indices

- Decision: Add private dictionaries on `CreatePetriNet` for place and transition reverse lookups (`name -> index`) and keep them synchronized with forward maps during each insertion.
- Rationale: Current lookup and duplicate checks use value scans (`ContainsValue`, `Where(...).First()`), which are O(n) in the hottest construction paths.
- Alternatives considered:
  - Add reverse maps to `GraphPetriNet` and `MatrixPetriNet`: broader scope and unnecessary API/ownership expansion for this feature.
  - Rebuild reverse maps on demand: simpler mutation story, but still incurs repeated O(n) work.

## Decision 2: Explicit failure semantics with idiomatic guards

- Decision: Use guard-first argument validation (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`) and explicit `KeyNotFoundException` for unresolved names/place IDs.
- Rationale: The constitution and FR-010 require explicit contracts in idiomatic C# and avoiding new Code Contracts usage in touched code.
- Alternatives considered:
  - Preserve exact `InvalidOperationException` shape from LINQ `First()`/`Single()`: minimal behavior delta but less explicit and not aligned with FR-010 guidance.
  - Add custom exception types: more expressive but unnecessary API surface for this incremental optimization.

## Decision 3: PNML lookup map for both Load and LoadMarkings

- Decision: Build one place-id-to-index dictionary per net during PNML processing and use it for arc and marking resolution in both `Load` and `LoadMarkings` pathways.
- Rationale: Existing code performs repeated value scans over places; a per-net lookup map removes this repeated O(n) cost while preserving semantics.
- Alternatives considered:
  - Keep loader scans and rely only on builder reverse maps: insufficient because loader currently resolves IDs from loaded net/place collections directly.
  - Full loader pipeline rewrite to a separate loader type: higher migration risk and outside this feature scope.

## Decision 4: Property-based invariant strategy

- Decision: Add FsCheck properties that validate bidirectional forward/reverse map consistency after generated sequences of `WithPlaces`, `WithTransitions`, `AddInArc`, and `AddOutArc` operations.
- Rationale: Constitution mandates property-based TDD and behavioral invariants, and SC-005 explicitly requires reverse-map consistency properties.
- Alternatives considered:
  - Example-style tests for a few fixed nets: does not satisfy constitution quality gate.
  - Reflection-only field checks without behavior checks: brittle and less representative of observable behavior.

## Decision 5: Benchmark strategy for performance and allocations

- Decision: Extend `perf/core.benchmarks` with deterministic construction and PNML load benchmark scenarios; include memory diagnostics to validate no bytes/op regressions beyond reverse map dictionaries.
- Rationale: SC-002/SC-003/SC-006 require measurable improvements with reproducible benchmark evidence.
- Alternatives considered:
  - Microbenchmark only `PlaceIndex`/`TransitionIndex`: too narrow for end-to-end construction/load acceptance criteria.
  - Ad hoc scripts outside BenchmarkDotNet: less reproducible and inconsistent with repository benchmarking practice.
