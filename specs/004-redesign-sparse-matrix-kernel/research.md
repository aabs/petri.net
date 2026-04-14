# Phase 0 Research: Dense-to-Sparse Matrix Representation Migration

## Decision 1: Use sparse incidence representation for storage and traversal

- Decision: Replace dense incidence traversal in `MatrixPetriNet` internals with a sparse incidence representation while preserving the same arithmetic semantics.
- Rationale: Sparse representations avoid full-grid traversal costs and can iterate non-zero links directly.
- Alternatives considered:
  - Keep dense matrices with local loop optimizations: insufficient for large sparse nets.
  - Introduce a custom sparse kernel type: higher complexity and maintenance risk.

## Decision 2: Build sparse-aware transition support traversal for fire and state-equation operations

- Decision: Maintain transition-indexed non-zero support derived from sparse incidence storage and derive affected-place sets from sparse non-zero enumeration.
- Rationale: This satisfies the requirement to skip guaranteed-empty paths and update only affected places.
- Alternatives considered:
  - Keep place-major full scans with conditional checks: still pays broad traversal cost.
  - On-demand recomputation from raw arc lists each fire: increases per-step overhead.

## Decision 3: Preserve weighted token delta correctness using sparse incidence values

- Decision: Use sparse incidence stored values as canonical incidence weights when computing token deltas.
- Rationale: The migration must preserve post-fire marking parity even when arc weights are greater than one.
- Alternatives considered:
  - Boolean-only adjacency representation: would break weighted-net correctness.
  - Parallel dense weight matrix retained for convenience: undermines sparse efficiency goal.

## Decision 4: Validate parity through differential oracle across baseline dense and sparse-backed paths

- Decision: Use property-based differential tests comparing baseline dense and migrated sparse kernels on identical generated models/markings/transition selections.
- Rationale: Internal representation change is high risk; correctness gate must cover marking, side-effect order, enablement/conflict parity, and determinism.
- Alternatives considered:
  - Example-only tests: insufficient behavioral coverage.
  - Benchmark-only validation: cannot establish semantic equivalence.

## Decision 5: Keep external MatrixPetriNet interface unchanged

- Decision: Restrict migration to internal representation and execution internals; keep public signatures and externally visible model shape stable.
- Rationale: Requested compatibility boundary requires callers to remain unaffected.
Alternatives considered:
  - Expose sparse-engine-specific API surfaces: increases coupling and breaks compatibility goals.
  - Introduce public dual-mode API now: out of scope for this migration.

## Decision 6: Sparse implementation remains internal-strategy flexible

- Decision: Keep representation choice implementation-agnostic at spec level (any sparse internal strategy is acceptable) while requiring parity and performance thresholds.
- Rationale: Clarified requirements prioritize observable behavior and measurable outcomes over a specific sparse engine.
- Alternatives considered:
  - Lock to a single sparse implementation in requirements: unnecessarily constrains implementation exploration.
  - Leave representation undefined without constraints: risks inconsistent parity/performance decisions.

## Decision 7: Keep dependency scope within current repository stack unless evidence requires expansion

- Decision: Prefer existing repository dependencies (including MathNet sparse capabilities) unless evidence requires an additional sparse engine.
- Rationale: Reduces dependency risk while preserving FR-010 flexibility for implementation choice.
- Alternatives considered:
  - Add additional sparse/set package for this migration: unnecessary if sparse matrix capabilities are sufficient.
  - Build custom sparse container types: unnecessary complexity for current scope.

## Decision 8: Benchmark evidence includes state-equation-focused sparse scenarios

- Decision: Extend BenchmarkDotNet scenarios to include state-equation and fire-path workloads across low/medium/high density with MemoryDiagnoser.
- Rationale: Migration claim is speedup in state-equation computation and sparse execution throughput while limiting dense regressions.
- Alternatives considered:
  - Fire-only benchmarks: misses stated state-equation target.
  - Single density benchmark: insufficient confidence in regression guardrails.

## Decision 9: Runtime fallback is not required after migration

- Decision: Do not require a permanent runtime fallback path to the dense baseline after migration completion.
- Rationale: Clarified spec accepts direct migration if parity and performance criteria are satisfied.
- Alternatives considered:
  - Keep long-term runtime toggle: adds enduring complexity and maintenance overhead.
  - Remove baseline comparison entirely: weakens parity verification confidence during migration.
