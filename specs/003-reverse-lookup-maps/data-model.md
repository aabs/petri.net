# Data Model: Reverse Lookup Maps

## Entity: CreatePetriNet

- Purpose: Mutable builder that accumulates places, transitions, arcs, markings, capacities, and transition callbacks before constructing `GraphPetriNet` or `MatrixPetriNet`.

### Fields (existing)

- `Name: string`
- `Places: Dictionary<int, string>` (forward place map)
- `Transitions: Dictionary<int, string>` (forward transition map)
- `PlaceMarkings: Dictionary<string, int>`
- `PlaceCapacities: Dictionary<string, int>`
- `InArcs: Dictionary<int, List<InArc>>`
- `OutArcs: Dictionary<int, List<OutArc>>`
- `TransitionFunctions: Dictionary<int, List<Action<GraphPetriNet>>>`

### Fields (new)

- `_placesByName: Dictionary<string, int>`
- `_transitionsByName: Dictionary<string, int>`

### Validation rules

- Place/transition names are non-null, non-empty, and non-whitespace.
- Name comparison semantics remain ordinal and case-sensitive.
- Duplicate names are ignored (existing behavior), preserving original assigned indices.
- Lookup methods throw on unresolved names with explicit exception messages.

### Invariants

- For every `(idx, name)` in `Places`, `_placesByName[name] == idx`.
- For every `(name, idx)` in `_placesByName`, `Places[idx] == name`.
- For every `(idx, name)` in `Transitions`, `_transitionsByName[name] == idx`.
- For every `(name, idx)` in `_transitionsByName`, `Transitions[idx] == name`.
- Insertion updates forward and reverse maps atomically within the same mutation step.

### State transitions

- `WithPlaces(names...)`
  - New names: append sequential index in `Places` and mirror in `_placesByName`.
  - Duplicate names: no-op.
- `WithTransitions(names...)`
  - New names: append sequential index in `Transitions` and mirror in `_transitionsByName`.
  - Duplicate names: no-op.
- `PlaceIndex(name)` / `TransitionIndex(name)`
  - Read-only lookup from reverse maps.
  - Missing key: throws contract-defined exception.

## Entity: PNML Place Lookup Context

- Purpose: Per-net lookup used during PNML parsing to resolve external place IDs to internal integer indices.

### Fields

- `placeIdToIndex: Dictionary<string, int>`

### Validation rules

- Built from the same place naming source used to initialize the builder/net.
- Missing place IDs fail fast with an actionable error message.

### Invariants

- Every place ID consumed by arc and marking resolution must map to exactly one index.
- Arc and marking resolution use this lookup map instead of scanning place value collections.

## Relationships

- `CreatePetriNet` reverse maps derive from and must stay consistent with forward maps.
- PNML lookup context is derived from a loaded net/builder place set and is scoped to one PNML net load.
- `GraphPetriNet` and `MatrixPetriNet` consume builder-produced forward maps unchanged; no public API expansion is required.
