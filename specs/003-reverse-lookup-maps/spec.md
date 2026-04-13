# Feature Specification: Reverse Lookup Maps for Names to Indices

**Feature Branch**: `003-reverse-lookup-maps`  
**Created**: 2026-04-13  
**Status**: Draft  
**Input**: User description: "R3: Reverse lookup maps for names to indices — Name-to-index resolution scans dictionaries by value, creating O(n) lookup overhead."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - O(1) Name-to-Index Resolution During Net Construction (Priority: P1)

When an engineer builds a Petri net using the builder API (e.g., `CreatePetriNet.Called(...).WithPlaces(...).WithTransitions(...).With(...).FedBy(...)...`), each arc addition resolves place and transition names to integer indices. Currently each resolution scans the entire dictionary by value, making every `PlaceIndex` and `TransitionIndex` call O(n). The builder must maintain reverse maps alongside the forward `Places` and `Transitions` dictionaries so that name-to-index resolution is O(1) regardless of net size.

**Why this priority**: This is the foundational change. Every arc added during net construction calls `PlaceIndex` and/or `TransitionIndex`. In large nets — telephony-scale systems with hundreds of places and transitions — this scan dominates build time and is called thousands of times per net construction. All other user stories and call sites depend on this reverse map being correct.

**Independent Test**: Can be fully tested by constructing nets of increasing size (small, medium, large) using the builder API and verifying that all resulting arcs correctly map to the expected place and transition indices. This delivers correct, O(1) lookup without any schema changes.

**Acceptance Scenarios**:

1. **Given** a builder with N places already registered, **When** `PlaceIndex("someName")` is called for a registered name, **Then** the returned index matches the forward dictionary entry and the call performs no linear scan of `Places.Values`.
2. **Given** a builder with N transitions already registered, **When** `TransitionIndex("someName")` is called for a registered name, **Then** the returned index matches the forward dictionary entry without a linear scan.
3. **Given** a net where the same place name is added a second time via `WithPlaces`, **When** arc additions subsequently resolve that name, **Then** the duplicate is ignored and the original index is returned consistently.
4. **Given** a builder under concurrent load (multiple arcs added to the same builder), **When** `PlaceIndex` and `TransitionIndex` are called from multiple threads, **Then** no data corruption or stale-read occurs (the reverse maps are updated atomically with the forward maps).

---

### User Story 2 - O(1) Duplicate Detection in WithPlaces and WithTransitions (Priority: P2)

The `WithPlaces` and `WithTransitions` builder methods currently check for duplicates using `ContainsValue`, which is also an O(n) scan. With a reverse map in place, duplicate detection becomes an O(1) key lookup, removing a secondary hot path during net construction.

**Why this priority**: This builds directly on the reverse maps introduced in User Story 1. It removes the only remaining value-scan in the two most frequently called builder methods. Without this change, duplicate detection remains O(n) even after the PlaceIndex/TransitionIndex fix.

**Independent Test**: Can be fully tested by registering N places/transitions and calling `WithPlaces`/`WithTransitions` with a mix of new names and duplicates. Correctness is verified if duplicate names are rejected and the forward and reverse maps agree on all registered names. No existing observable behavior should change.

**Acceptance Scenarios**:

1. **Given** a builder where place `"p1"` is already registered, **When** `WithPlaces("p1", "p2")` is called, **Then** `"p1"` is not re-added and `"p2"` receives the next sequential index.
2. **Given** a builder where transition `"t1"` is already registered, **When** `WithTransitions("t1", "t2")` is called, **Then** `"t1"` is not re-added and `"t2"` receives the next sequential index.
3. **Given** N places already registered, **When** `WithPlaces` is called with one new and one duplicate name, **Then** the forward and reverse maps are consistent and contain exactly N+1 entries.

---

### User Story 3 - PNML Loader Uses Reverse Maps for Place Lookups (Priority: P2)

The PNML loader (`PnmlModelLoader`) resolves PNML place IDs to integer indices using `net.Places.Where(x => x.Value == Id).Single()`. For each arc in the PNML file, this is an O(n) value scan over the places dictionary. The loader must be updated to use a reverse lookup exposed from the loaded `GraphPetriNet` so that this resolution is O(1).

**Why this priority**: PNML-loaded nets are the primary production entry point for telephony-scale models that may have thousands of places and hundreds of arcs. Startup/reload time is directly affected by these scans. This change is a prerequisite for the load-time improvement in the acceptance criteria.

**Independent Test**: Can be fully tested by loading a PNML file with an increasing number of places and net elements, asserting that all place IDs are resolved correctly, and confirming via benchmark that load time does not grow quadratically with place count.

**Acceptance Scenarios**:

1. **Given** a PNML file with M places and A arcs, **When** the loader constructs `GraphPetriNet` instances, **Then** every arc source is resolved to the correct integer index without scanning `Places.Values`.
2. **Given** a PNML file containing a net with N places, **When** `LoadMarkings` resolves place IDs to indices for each marking entry, **Then** all indices are correct, and the operation does not perform N linear scans.
3. **Given** a PNML file with a place ID that does not match any registered place, **When** the loader attempts to resolve it, **Then** the loader throws `KeyNotFoundException` with a clear, actionable message.

---

### Edge Cases

- What happens when `PlaceIndex` or `TransitionIndex` is called for a name not in the reverse map? The method must throw `KeyNotFoundException` with a clear, actionable message.
- What happens when a builder is cloned or serialized? No clone or serialize API is currently in scope for this feature; if such a pathway is introduced later, reverse maps must be reconstructed from forward maps when not persisted.
- What happens when a builder is used for both `GraphPetriNet` and `MatrixPetriNet` construction via `CreateNet<T>`? The reverse maps are on the builder, not the net, so both construction paths benefit without any model-level changes.
- What happens with very large nets where place names contain Unicode or spaces? The reverse map keys must use the same comparison semantics as the existing `ContainsValue` and `Where(...).First()` calls (ordinal string equality).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The `CreatePetriNet` builder MUST maintain a `Dictionary<string, int>` reverse map for places (`_placesByName`) that is updated atomically with the `Places` forward map at every point of insertion.
- **FR-002**: The `CreatePetriNet` builder MUST maintain a `Dictionary<string, int>` reverse map for transitions (`_transitionsByName`) that is updated atomically with the `Transitions` forward map at every point of insertion.
- **FR-003**: `PlaceIndex(string name)` MUST resolve the index via the reverse map in O(1), and MUST throw `KeyNotFoundException` when the name is not registered.
- **FR-004**: `TransitionIndex(string name)` MUST resolve the index via the reverse map in O(1), and MUST throw `KeyNotFoundException` when the name is not registered.
- **FR-005**: `WithPlaces` MUST replace the `ContainsValue(item.Value)` duplicate check with an O(1) reverse-map key lookup.
- **FR-006**: `WithTransitions` MUST replace the `ContainsValue(item.Value)` duplicate check with an O(1) reverse-map key lookup.
- **FR-007**: The `PnmlModelLoader` MUST resolve place IDs to indices through the reverse map rather than scanning `net.Places.Values`, for both the `Load` and `LoadMarkings` entry points.
- **FR-008**: The reverse maps MUST remain consistent with the forward maps at all times. An invariant property test MUST verify this consistency after net construction.
- **FR-009**: The public builder API, including all `With`, `FedBy`, `Feeding`, `AndPlaces`, `AndTransitions`, `WithPlaces`, `WithTransitions`, `PlaceIndex`, `TransitionIndex`, `AddInArc`, and `AddOutArc` signatures, MUST remain unchanged.
- **FR-010**: System MUST define the contract-sensitive behavior for inputs, outputs, invariants, and failure modes using idiomatic C# guard clauses (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`, `KeyNotFoundException`) rather than relying on deprecated Code Contracts tooling in new or touched code.

### Key Entities

- **`CreatePetriNet` builder**: The mutable builder accumulating places, transitions, and arcs before net construction. Owns both the forward (`id→name`) and new reverse (`name→id`) maps.
- **`Places` (forward map)**: `Dictionary<int, string>` — maps integer index to place name. Unchanged in type and semantics.
- **`Transitions` (forward map)**: `Dictionary<int, string>` — maps integer index to transition name. Unchanged in type and semantics.
- **`_placesByName` (reverse map)**: `Dictionary<string, int>` — maps place name to integer index. New field on `CreatePetriNet`. Internal to the builder.
- **`_transitionsByName` (reverse map)**: `Dictionary<string, int>` — maps transition name to integer index. New field on `CreatePetriNet`. Internal to the builder.
- **`PnmlModelLoader`**: Static loader consuming PNML XML and producing `GraphPetriNet` instances. Will consume the reverse map exposed from the constructed net or from an intermediate builder-level lookup during load.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Name-to-index resolution in the builder (`PlaceIndex`, `TransitionIndex`) executes in constant time regardless of net size. For a net with 1,000 places, lookup time must not exceed that of a 10-place net by more than a factor proportional to hash overhead (effectively flat, not linear).
- **SC-002**: Net construction time for a large net (1,000 places + 5,000 transitions + 10,000 arcs) improves by at least 25% compared to the baseline, as measured by the construction microbenchmark in `perf/core.benchmarks`.
- **SC-003**: PNML load time for a large model (>500 places, >500 arcs) does not grow quadratically with place count, and large-model load time improves by at least 25% compared to the baseline as measured with a synthetic PNML benchmark.
- **SC-004**: All existing property tests and regression tests pass without modification, confirming that no observable behavior changed.
- **SC-005**: A new reverse-map consistency property test confirms that `Places[i] == name` if and only if `_placesByName[name] == i`, and likewise for transitions, for all builder states after any sequence of `WithPlaces` / `WithTransitions` / `AddInArc` / `AddOutArc` calls.
- **SC-006**: The allocation profile (bytes/op) in the construction benchmark does not regress: the reverse maps must not introduce excess intermediate allocation beyond the two dictionary objects themselves.

When performance is a goal, the correctness-preserving boundary comes first: the reverse maps must agree with the forward maps under all inputs before any performance claim is valid.

## Assumptions

- The `CreatePetriNet` builder may be accessed concurrently for arc additions and name-to-index lookups. Forward and reverse maps must stay synchronized under concurrent access, and this feature includes the required synchronization hardening.
- Existing call sites may still use builders single-threadedly, but this feature does not rely on that usage pattern for correctness.
- Place names and transition names are compared with ordinal (case-sensitive) string equality, matching the behavior of the existing `ContainsValue`, `Where(...).First()`, and `Single()` patterns.
- `MatrixPetriNet` construction is also done via the `CreatePetriNet` builder (`CreateNet<T>`), so it benefits from the same improvement without any model-level change.
- PNML-loaded nets only use `GraphPetriNet` as of the current loader; the reverse map approach generalizes to `MatrixPetriNet` if that loader path is added later.
- `CreatePetriNet.Places` and `CreatePetriNet.Transitions` are `public` mutable properties. The reverse maps will be maintained as internal fields to avoid exposing additional mutable state through the public API. Callers who directly mutate `Places` or `Transitions` outside the builder methods are considered to be violating the builder contract and are out of scope.
- Benchmark scenarios for large net construction must be committed to `perf/core.benchmarks` and be deterministic and reproducible from source control.
