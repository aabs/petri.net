# Contract: PNML Streaming Loader

## Scope

Defines external behavior and failure contracts for the new streamed PNML loader, the new `PetriNetBuilder`, and deprecation boundary for the existing loader.

## PetriNetBuilder Surface

### Proposed type

- `PetriNetBuilder` in `src/core/builders/PetriNetBuilder.cs`

### Fluent API idiom contract

- Builder methods follow conventional fluent idioms inspired by fifthlang-style generated builders:
  - `WithName(...)`, `WithPlaces(...)`, `WithTransitions(...)`
  - `AddingPlace(...)`, `AddingTransition(...)`, `AddingArc(...)` style incremental helpers
  - Terminal materialization methods such as `BuildGraph()` and `BuildMatrix()`
- All fluent methods return the same builder instance (`this`) for chaining.
- Null/invalid arguments fail fast with idiomatic guard-based exceptions.

### Representation contract

- Intermediate builder state is sparse and append-oriented (ID maps and adjacency collections), not eagerly dense.
- `BuildGraph`/`BuildMatrix` materialize dense final structures in one controlled projection step.
- Materialization avoids unnecessary temporary collections and repeated reallocation.

## New Loader Surface

### Proposed type

- `PnmlStreamingModelLoader`

### Core operation contract

- Input:
  - `path` MUST be non-null, non-whitespace, and point to a readable PNML file.
  - Caller selects target implementation: Graph or Matrix.
- Behavior:
  - Parse PNML with forward-only `XmlReader` in a single document pass.
  - Construct nets during parsing.
  - Return all supported PT-net `<net>` elements in source order.
  - Use deterministic ordinal ID resolution for places/transitions/arcs.
- Output:
  - Ordered collection of nets in selected target representation.

### Finalized API names

- `IReadOnlyList<GraphPetriNet> LoadGraph(string path)`
- `IReadOnlyList<MatrixPetriNet> LoadMatrix(string path)`
- `IReadOnlyList<TNet> Load<TNet>(string path) where TNet : PetriNetBase`
- `IReadOnlyList<PetriNetBase> Load(string path, PnmlTargetModel target)`

## Conformance Boundary

- Supported in this feature:
  - Core PT-net subset required by current domain behavior: net, place, transition, arc, and initial marking.
  - Common metadata wrappers that do not alter core semantics.
- Out of scope in this feature:
  - Non-core PNML extension semantics beyond existing behavior.

## Failure Contract

- Invalid path / unreadable file -> argument or I/O exception with actionable message.
- Missing required identifiers (`net`, `place`, `transition`) -> deterministic exception with context.
- Duplicate place/transition IDs within a net -> deterministic exception with identifier and net context.
- Arc endpoint cannot be resolved -> deterministic exception including arc/net and missing endpoint ID.
- Malformed required core structure -> deterministic parse/loader exception with context.

## Ordering Contract

- Returned net order MUST match source PNML document order.
- Multi-net documents are valid input; all supported nets are returned.

## Target Type Contract

- Caller can choose Graph-based or Matrix-based net materialization via new loader API.
- Loader target selection maps directly to `PetriNetBuilder` terminal materialization path.
- Both output modes MUST preserve equivalent externally observable behavior on valid conformant input.

## Compatibility and Deprecation Contract

- Existing `PnmlModelLoader` remains functionally unchanged in this feature.
- Existing `PnmlModelLoader` is marked deprecated with migration guidance to new loader.
- Existing `CreatePetriNet` remains available as compatibility path; `PetriNetBuilder` is the preferred builder for the new streaming flow.
- Existing tests for old loader remain valid unless explicitly superseded by feature scope.

## Test and Validation Contract

- Existing embedded PNML corpus in `test/core.tests/standard test nets/*.xml` is mandatory regression input for new loader conformance tests.
- Property-based tests MUST cover `PetriNetBuilder` invariants for name/index uniqueness, arc endpoint validity, and graph/matrix build parity.
- Property-based tests MUST verify behavioral invariants/parity instead of single canned examples.
- Performance claims require reproducible benchmark evidence for throughput and allocation deltas, including builder construction memory profiles.
