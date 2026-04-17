# Data Model: Streamed PNML Loader

## Entity: PnmlStreamingModelLoader

- Purpose: Forward-only PNML loader that parses with `XmlReader` and constructs nets during parse.
- Responsibilities:
  - Parse PNML document in single pass.
  - Build one or more nets in document order.
  - Materialize selected target implementation (Graph or Matrix) per caller request.
  - Produce deterministic failures for invalid core PT-net input.

### Key Fields / Components

- `XmlReaderSettings` profile for safe, deterministic parsing.
- `TargetNetKind` selector (Graph/Matrix) or equivalent type-driven selection.
- Internal per-net parse state (`NetBuildSession`).

## Entity: PetriNetBuilder

- Purpose: New conventional fluent builder in `src/core/builders/PetriNetBuilder.cs` used by streaming loader and general incremental net construction.
- Responsibilities:
  - Expose clear fluent mutation methods (`With*`, `Adding*`) returning the builder.
  - Track place/transition identity and arc data with low-overhead intermediate state.
  - Materialize final `GraphPetriNet` and `MatrixPetriNet` with minimized allocation churn.

### Fields

- `Name: string`
- `PlaceNameToIndex: Dictionary<string, int>`
- `TransitionNameToIndex: Dictionary<string, int>`
- `PlaceIndexToName: List<string>`
- `TransitionIndexToName: List<string>`
- `SparseInArcsByTransition: Dictionary<int, List<InArcSpec>>`
- `SparseOutArcsByTransition: Dictionary<int, List<OutArcSpec>>`
- `InitialMarkingsByPlace: Dictionary<int, int>`
- `CapacitiesByPlace: Dictionary<int, int>`

### Validation Rules

- Place/transition names are required and ordinal-unique within one builder instance.
- Arc endpoints must resolve to known place/transition IDs before finalization completes.
- Arc weights are positive.

### Invariants

- Forward/reverse name-index maps are bijectively consistent.
- Sparse adjacency contains only valid endpoints.
- Finalization preserves all inserted nodes/arcs without duplication or omission.

## Entity: InArcSpec

- Purpose: Compact intermediate in-arc representation for sparse builder state.

### Fields

- `SourcePlaceIndex: int`
- `Weight: int`
- `IsInhibitor: bool`

## Entity: OutArcSpec

- Purpose: Compact intermediate out-arc representation for sparse builder state.

### Fields

- `TargetPlaceIndex: int`
- `Weight: int`

## Entity: NetBuildSession

- Purpose: Mutable state for one active `<net>` element during stream processing.
- Lifetime: Created on net start, finalized on net end.

### Fields

- `NetId: string`
- `NetType: string`
- `Builder: PetriNetBuilder`
- `PlaceIdToName: Dictionary<string, string>` (ordinal)
- `TransitionIdToName: Dictionary<string, string>` (ordinal)
- `ArcBuffer: List<PendingArc>` or immediate arc application state
- `InitialMarkings: Dictionary<string, int>`

### Validation Rules

- `NetId` is required and non-whitespace.
- `NetType` must map to supported core PT-net values for this feature.
- Duplicate place IDs and duplicate transition IDs are invalid.
- Arc endpoints must reference previously or eventually defined IDs according to chosen buffering strategy; unresolved references fail deterministically by net finalization.

### Invariants

- Each place ID and transition ID is unique within a net.
- Arc endpoint resolution uses ordinal ID lookups.
- Finalized net preserves source document ordering among all loaded nets.

## Entity: PendingArc

- Purpose: Represents an arc not yet fully applied due to endpoint ordering in stream.

### Fields

- `ArcId: string`
- `SourceId: string`
- `TargetId: string`
- `Weight: int`
- `IsInhibitor: bool`

### Validation Rules

- `ArcId` may be optional in input, but diagnostic failures must include fallback context.
- Weight defaults follow existing loader semantics when inscription is missing.
- Weight must be positive for valid arc creation.

## Entity: LoaderResult

- Purpose: Ordered set of constructed nets returned by load operation.

### Fields

- `Nets: IReadOnlyList<TNet>` where `TNet` is either `GraphPetriNet` or `MatrixPetriNet` selected by loader API

### Invariants

- Number of returned nets equals number of accepted PT-net `<net>` elements.
- Return order matches source document order.

## State Transitions (Single Net)

1. `Idle` -> `InNet`: encounter supported `<net>` start; initialize `NetBuildSession`.
2. `InNet` -> `ReadingNodes`: parse `<place>` and `<transition>`; update ID maps and builder registrations.
3. `ReadingNodes`/`InNet` -> `ReadingArcs`: parse `<arc>` elements, resolve/apply or buffer.
4. `InNet` -> `Finalizing`: validate unresolved references, apply pending arcs into sparse adjacency, materialize selected dense net type.
5. `Finalizing` -> `Completed`: append net to result and clear session.

## State Transitions (Builder Materialization)

1. `SparseCollecting` -> `Validating`: ensure endpoint/name/index consistency.
2. `Validating` -> `DenseProjection`: allocate final place/transition dictionaries and arc lists sized from sparse counts.
3. `DenseProjection` -> `NetConstructed`: construct `GraphPetriNet` or `MatrixPetriNet` with minimal intermediate allocations.

## Relationships

- `PnmlStreamingModelLoader` manages many `NetBuildSession` instances sequentially.
- Each `NetBuildSession` composes a `PetriNetBuilder` and auxiliary PNML ID maps.
- `PendingArc` is owned by `NetBuildSession`.
- `InArcSpec` and `OutArcSpec` are owned by `PetriNetBuilder` sparse adjacency collections.
- `LoaderResult` aggregates finalized net instances in order.

## Contract-Sensitive Failure Modes

- Missing required net/place/transition identifiers -> actionable exception with net context.
- Duplicate node identifiers -> actionable exception with identifier and net context.
- Arc references unknown endpoint IDs -> actionable exception with arc and net context.
- Malformed required core PT-net structure -> deterministic parse failure mapped to loader contract.
