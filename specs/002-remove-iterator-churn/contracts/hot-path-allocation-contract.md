# Hot-Path Allocation Contract

## Scope

This contract defines the required observable behavior for the R2 hotspot changes in `PetriNetBase`, `GraphPetriNet`, `MatrixPetriNet`, and `Marking`.

## Inputs

- A valid `Marking` instance representing token counts for the current net state.
- A Petri net with zero or more places, transitions, arcs, optional priorities, and optional registered transition functions.
- Existing internal enumerations of places, transitions, arcs, and markings that may currently materialize temporary collections or traverse the same data more than once.

## Outputs

- Enablement checks continue to return the same boolean result for the same inputs.
- Conflict checks continue to return the same boolean result for the same inputs.
- Firing operations continue to return the same resulting `Marking` for the same starting state and selected transitions.
- Transition-function dispatch continues to invoke the same handlers in the same observable order.
- Hotspot implementations may change their internal iteration shape, but the public and externally observable outcomes must remain equivalent.

## Behavioral Rules

1. Removing temporary materialization must not change whether a transition is considered enabled.
2. Reducing repeated traversal must not change whether a place is considered conflicted.
3. Firing a marking must continue to produce the same token updates as the pre-change behavior.
4. Transition-function invocation count and order must remain unchanged for equivalent firing steps.
5. Methods that previously returned an empty result or no-op behavior for empty inputs must continue to do so.
6. Public API signatures and externally observable return shapes must remain unchanged.
7. This feature must not introduce reverse lookup maps, PNML traversal redesign, sparse matrix kernel redesign, or in-place marking semantics changes.

## Compatibility Requirements

- The behavior of `PetriNetBase.IsConflicted`, `PetriNetBase.PlaceIsConflicted`, `PetriNetBase.GetEnabledTransitionsAdjacentToPlace`, `GraphPetriNet.CreateFiringPlan`, `GraphPetriNet.Fire`, `MatrixPetriNet.CreateFiringPlan`, `MatrixPetriNet.Fire`, and `Marking` construction/copy paths must remain equivalent for the same inputs.
- Existing tests that cover graph and matrix parity must remain valid without changing their user-visible expectations.
- Contract clarification for this feature must not add new deprecated Code Contracts usage.

## Verification

- FsCheck.Xunit properties must cover unchanged enablement, unchanged firing results, unchanged invocation order, and unchanged empty-input behavior.
- Benchmarks must exist for affected hotspots in the committed `perf/core.benchmarks` project.
- Allocation profiling must show that each changed hotspot reduces or eliminates the targeted materialization or iterator-churn source relative to baseline.

## Verification evidence (2026-04-12)

- Regression validation passed via `dotnet test petrinets2.slnx` with zero failures.
- Hotspot benchmark validation passed via `HotPathAllocationBenchmarks` in `perf/core.benchmarks` covering:
	- graph conflict path
	- matrix conflict path
	- graph fire path
	- matrix fire path
- Benchmark observations showed lower latency/allocation in graph conflict and fire paths compared to matrix analogues for the sampled scenario sizes.
- `Marking` copy behavior remains measurable in fire-path allocations and is captured as supporting context for this feature rather than a semantic redesign target in R2.