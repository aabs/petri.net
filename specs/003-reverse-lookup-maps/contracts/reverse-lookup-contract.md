# Contract: Reverse Lookup Behavior

## Scope

This contract defines externally observable behavior and internal invariants for reverse lookup adoption in builder and PNML loading paths.

## Builder Contract (`CreatePetriNet`)

### Method: `WithPlaces(params string[] placeNames)`

- Inputs:
  - `placeNames` must be non-null and contain only non-null, non-whitespace, alphanumeric names (existing behavior constraints).
- Effects:
  - For each unseen name, append one new sequential index to `Places`.
  - Insert matching reverse entry into `_placesByName` in the same mutation step.
  - Duplicate names are ignored.
- Output:
  - Returns the same builder instance.
- Failure modes:
  - Invalid arguments fail fast via guard/contract checks.

### Method: `WithTransitions(params string[] transitionNames)`

- Inputs:
  - `transitionNames` must be non-null and contain only non-null, non-whitespace, alphanumeric names.
- Effects:
  - For each unseen name, append one new sequential index to `Transitions`.
  - Insert matching reverse entry into `_transitionsByName` in the same mutation step.
  - Duplicate names are ignored.
- Output:
  - Returns the same builder instance.
- Failure modes:
  - Invalid arguments fail fast via guard/contract checks.

### Method: `PlaceIndex(string name)`

- Inputs:
  - `name` must be non-null and non-whitespace.
- Effects:
  - Performs O(1) lookup against `_placesByName`.
- Output:
  - Returns registered index for `name`.
- Failure modes:
  - If not registered, throws `KeyNotFoundException` with actionable context.

### Method: `TransitionIndex(string name)`

- Inputs:
  - `name` must be non-null and non-whitespace.
- Effects:
  - Performs O(1) lookup against `_transitionsByName`.
- Output:
  - Returns registered index for `name`.
- Failure modes:
  - If not registered, throws `KeyNotFoundException` with actionable context.

### Invariants

- `Places` and `_placesByName` remain bijectively consistent.
- `Transitions` and `_transitionsByName` remain bijectively consistent.
- Public API signatures remain unchanged.

## PNML Loader Contract (`PnmlModelLoader`)

### Entry Points: `Load(string modelPath)` and `LoadMarkings(GraphPetriNet net, string modelPath)`

- Inputs:
  - `modelPath` must refer to an existing readable PNML file.
  - `net` argument for `LoadMarkings` must be non-null and correspond to the PNML model identity.
- Effects:
  - Place IDs are resolved with lookup-map semantics (`id -> index`) and not through repeated value scans.
  - Arc and marking resolution maintain existing semantic outcomes.
- Output:
  - Produces equivalent `GraphPetriNet` and `Marking` results relative to current behavior.
- Failure modes:
  - Missing place IDs fail fast with `KeyNotFoundException` and clear actionable errors.
  - Malformed/missing model files preserve existing loader failure behavior class.

## Non-Goals

- No change to public builder fluent surface or signatures.
- No behavioral change to firing semantics, ordering semantics, or graph/matrix parity.
- No expansion of deprecated `System.Diagnostics.Contracts` usage.
