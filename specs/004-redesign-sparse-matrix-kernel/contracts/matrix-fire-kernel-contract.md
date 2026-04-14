# Contract: Dense-to-Sparse Matrix Representation

## Scope

Defines externally observable behavior and validation boundaries for baseline reference and migrated sparse-backed matrix fire/state-equation execution paths.

## Surface and Compatibility

- Public APIs and externally visible model shape remain unchanged.
- The migration is internal to matrix representation and execution logic.
- Behavior parity is measured against baseline mode for identical inputs.

## Input Contract

- Fire execution accepts a net definition, marking, and selected transition set.
- State-equation computation accepts a net definition and marking.
- Transition indices and place indices must be valid for the active net.
- Null and invalid inputs are handled with existing idiomatic C# guard semantics.

## Behavioral Contract

1. Sparse-backed execution enumerates only known non-zero connectivity from sparse incidence support.
2. Sparse-backed execution skips disconnected rows and columns for active transitions.
3. Sparse-backed execution does not enumerate known zero or non-existent paths.
4. Token deltas are computed and applied only for affected places, using preserved weighted incidence values.
5. Post-fire marking is equivalent to baseline for identical inputs.
6. State-equation outputs are exactly equal to baseline for identical inputs (full result-value equality).
7. Transition side-effect invocation order is equivalent to baseline.
8. Enablement and conflict outcomes are equivalent to baseline.
9. Execution remains deterministic for repeated identical inputs.

## Failure-Mode Contract

- Null inputs fail fast with explicit idiomatic C# argument exceptions.
- Invalid transition or place indices fail fast with explicit range or key-based exceptions.
- Marking/net cardinality mismatch fails fast with explicit argument/operation exceptions before computation.
- Missing sparse connectivity entries are treated as zero incidence values (not failures) unless violating explicit input contracts.
- Tie-breaking behavior for transition selection is unchanged from baseline and is therefore not redefined by this feature.
- No new failure modes are introduced at the public API boundary.

## Migration Contract

- Migration is complete when sparse-backed execution satisfies parity and performance criteria.
- Permanent runtime fallback to dense baseline is not required after migration acceptance.

## Validation Contract

- Property-based differential oracle validates behavioral parity across generated sparse and dense families.
- FR-015 validation uses exact state-equation result-value equality checks (no epsilon/tolerance mode).
- Existing correctness suite must pass without regressions.
- Benchmarks must demonstrate >=2.0x sparse throughput gain for fire workloads.
- Benchmarks must demonstrate >=2.0x sparse throughput gain for state-equation workloads.
- Dense regression must be <=5% for both fire and state-equation workloads.
- FR-014 benchmark acceptance gates are defined by low/medium/high density bands and do not require fixed place-transition tuple sizes.
- Allocation behavior in sparse workloads must be non-increasing or benchmark-justified.
