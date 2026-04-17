# Phase 0 Research: XmlReader-Based PNML Streaming Loader

## Decision 1: Use `System.Xml.XmlReader` as the parser backbone

- Decision: Build the new loader on the BCL forward-only parser `System.Xml.XmlReader` with explicit element-state handling.
- Rationale: `XmlReader` provides pull-based, low-allocation parsing suitable for large PNML files and aligns with the feature requirement for single-pass SAX-style processing.
- Alternatives considered:
  - `XDocument`/LINQ to XML: simpler querying but materializes tree state and increases allocations.
  - Third-party SAX wrappers: unnecessary dependency and less consistent with repository preference for modern BCL-first solutions.

## Decision 2: Introduce a new loader class and deprecate the old loader

- Decision: Add a new class (planned as `PnmlStreamingModelLoader`) for streamed loading and leave `PnmlModelLoader` functionally untouched but marked deprecated.
- Rationale: Minimizes migration risk and preserves backward compatibility while enabling a new optimized flow.
- Alternatives considered:
  - In-place rewrite of `PnmlModelLoader`: higher regression risk and weak migration story.
  - Keeping both loaders without deprecation signal: creates API ambiguity and prolongs accidental usage of old path.

## Decision 3: Support Graph/Matrix selection through the new loader API

- Decision: New loader API explicitly allows caller choice of target net implementation (`GraphPetriNet` or `MatrixPetriNet`) per load operation.
- Rationale: Satisfies feature request and keeps construction logic centralized around existing builder/domain pathways.
- Alternatives considered:
  - Graph-only streamed loader: does not satisfy requested scope.
  - Separate classes per target type only: possible, but less ergonomic than one API with target selection.

## Decision 4: Add a new conventional `PetriNetBuilder` under `src/core/builders`

- Decision: Introduce `PetriNetBuilder` with explicit fluent methods patterned after conventional builder idioms (`With*`, `Adding*`, and terminal build methods) rather than expanding legacy `CreatePetriNet` surface for all new streaming behavior.
- Rationale: Provides a clean, incremental-construction API for streamed parse events, aligns with requested fifthlang-style ergonomics, and reduces coupling to legacy builder patterns.
- Alternatives considered:
  - Reuse only `CreatePetriNet`: lower immediate churn but weaker API clarity for streaming event-by-event construction.
  - Replace `CreatePetriNet` outright: unnecessary migration risk; compatibility path is preferable.

## Decision 5: Use sparse intermediate adjacency state and dense finalization

- Decision: Store incremental net description in sparse intermediate structures (ID maps and per-transition adjacency lists) and convert once into dense structures required by final net models during `BuildGraph`/`BuildMatrix`.
- Rationale: Streaming ingestion is sparse and append-heavy; this avoids eager dense allocations and reduces temporary object churn, while still producing efficient final representations.
- Alternatives considered:
  - Keep fully dense state during incremental construction: simpler finalization but higher steady-state allocation/memory pressure.
  - Keep sparse state all the way into runtime models: risks semantic and performance divergence from current model expectations.

## Decision 6: Preserve conformance boundary at core PT-net subset

- Decision: Validate and guarantee conformance for core PT-net constructs needed by current domain behavior: net, place, transition, arc, and initial marking, including namespace-aware parsing.
- Rationale: Matches clarified feature scope and prevents uncontrolled expansion into non-core extension semantics.
- Alternatives considered:
  - Broad PNML extension support in this feature: too large and under-specified for this iteration.
  - Sample-only compatibility with no conformance framing: too weak for future maintainability.

## Decision 7: Reuse existing embedded sample corpus as conformance baseline

- Decision: Reuse the exact embedded XML corpus in `test/core.tests/standard test nets/*.xml` currently consumed by loader tests as a mandatory regression set for the new loader.
- Rationale: Ensures backward behavior parity using proven inputs and satisfies explicit feature constraint.
- Alternatives considered:
  - New synthetic corpus only: risks missing existing edge behavior already captured by current samples.
  - Manual spot checks: non-repeatable and insufficient for CI confidence.

## Decision 8: Keep performance claims evidence-driven

- Decision: Validate success criteria with BenchmarkDotNet scenarios focused on PNML load throughput and allocations for old vs new loader paths, plus builder construction allocation profiles.
- Rationale: Constitution requires correctness before performance and measurable reproducible evidence for optimization claims.
- Alternatives considered:
  - Relying on wall-clock local measurements only: not reproducible enough.
  - Optimizing without benchmark deltas: non-compliant with performance governance.
