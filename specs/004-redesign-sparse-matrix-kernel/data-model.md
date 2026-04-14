# Data Model: Sparse Representation Migration

## Entity: Fire Input State

- Purpose: Complete input context for one matrix fire/state-equation evaluation.

### Fields

- `NetDefinition`: Internal sparse representation metadata and weighted incidence storage.
- `CurrentMarking`: Token counts before execution.
- `SelectedTransitions`: Transition indices chosen for execution.

### Validation rules

- `SelectedTransitions` reference valid transition indices.
- `CurrentMarking` cardinality matches place cardinality.

### Invariants

- For deterministic transition functions, identical `Fire Input State` yields deterministic outcomes.
- Baseline reference and migrated sparse-backed execution are equivalent evaluators of the same observable contract.

## Entity: Sparse Transition Support Index

- Purpose: Sparse connectivity index for non-zero incidence positions.

### Fields

- `PreSupportByTransition`: `transition -> sparse place-index set` where pre-incidence weight is non-zero.
- `PostSupportByTransition`: `transition -> sparse place-index set` where post-incidence weight is non-zero.
- `EffectiveSupportByTransition`: Cached or derived sparse union used to determine affected places.

### Validation rules

- Entries reference valid place indices.
- Place index appears in support set iff corresponding incidence weight is non-zero.

### Invariants

- Enumerating support sets never yields non-existent or zero-valued paths.
- Empty-transition support yields empty set, not full-matrix scan behavior.

## Entity: Sparse Incidence Store

- Purpose: Preserve exact arithmetic semantics for token delta computation with sparse storage.

### Fields

- `PreIncidenceSparse`: Sparse representation of pre-incidence weights.
- `PostIncidenceSparse`: Sparse representation of post-incidence weights.

### Validation rules

- Non-zero values exist only for paths included in corresponding support sets.
- Missing sparse entries are treated as zero values.

### Invariants

- Token delta arithmetic uses exact sparse incidence values equivalent to baseline matrix semantics.
- Weighted arcs with values > 1 preserve baseline behavior.

## Entity: Affected Place Set

- Purpose: Minimal place set that can change given selected transitions.

### Fields

- `PlaceIndices`: Union of effective sparse support across selected transitions.

### Validation rules

- No duplicates by construction.
- Includes all and only places with potential non-zero delta.

### Invariants

- Token updates are applied only to places in `PlaceIndices`.
- Places outside set are guaranteed unchanged for this execution step.

## Entity: Fire Evaluation Result

- Purpose: Observable output for parity and determinism verification.

### Fields

- `PostFireMarking`
- `StateEquationResult`
- `EnablementOutcome`
- `ConflictOutcome`
- `TransitionInvocationTrace`

### Validation rules

- Result shape remains identical between baseline dense and sparse-backed modes.

### Invariants

- For identical input state, baseline dense and sparse-backed results are equivalent.
- `TransitionInvocationTrace` preserves baseline invocation order.

## Relationships

- `Sparse Transition Support Index` bounds traversal and affected-place discovery.
- `Sparse Incidence Store` supplies arithmetic values for paths surfaced by support sets.
- `Fire Input State` + selected transitions derive `Affected Place Set` and final `Fire Evaluation Result`.
- Baseline reference evaluation is retained for parity checks during migration and validation.
