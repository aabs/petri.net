# Data Model: Hot-Path Allocation Removal

## Overview

This feature does not introduce persisted data. The relevant model is the in-memory hotspot state, the transient materialization and traversal sites being removed, and the benchmark/property inputs used to prove correctness-preserving performance improvement.

## Entities

### Hotspot Slice

- Description: A bounded subset of the R2 scope selected for one implementation increment and one validation pass.
- Fields:
  - `SliceName` (`string`): Human-readable identifier such as `PetriNetBase-enablement` or `Matrix-fire-loop`.
  - `Locations` (`string[]`): Source locations included in the slice.
  - `HotspotKind` (`Enablement | Conflict | Firing | Marking`): The dominant execution concern for the slice.
  - `ExpectedAllocationIssue` (`string`): Short description of the avoidable materialization or iterator churn.
- Validation rules:
  - Each slice must stay within the R2 hotspot scope.
  - Each slice must be measurable in isolation through tests and benchmarks.

### Materialization Site

- Description: A hotspot location where temporary lists, arrays, cloned state, or equivalent transient objects are created only to support immediately following logic.
- Fields:
  - `Location` (`string`): Source path and line anchor.
  - `SourceSequence` (`string`): The data being materialized, such as enabled transitions, arcs, or marking state.
  - `Purpose` (`string`): Why the materialization exists today.
  - `ReplacementShape` (`string`): The intended non-materializing behavior, such as direct iteration or reuse of an existing sequence.
- Validation rules:
  - Removing the materialization must not change externally observable behavior.
  - The replacement must still satisfy invocation-order and token-update constraints.

### Repeated Traversal Site

- Description: A hotspot location where the same transition, arc, or marking data is traversed more than once during a single decision or firing step.
- Fields:
  - `Location` (`string`): Source path and line anchor.
  - `TraversedData` (`string`): Transition IDs, arcs, places, or marking values.
  - `CurrentPassCount` (`string`): Qualitative description of current repeated passes.
  - `TargetTraversalShape` (`string`): The intended single-pass or bounded-pass replacement.
- Validation rules:
  - The reduced traversal shape must preserve enablement and firing results.
  - Early exits or loop fusion must not change the set or order of visible transition-function invocations.

### Allocation Benchmark Scenario

- Description: A reproducible benchmark input used to quantify allocation and latency effects for an R2 hotspot.
- Fields:
  - `ScenarioName` (`string`): Benchmark identifier.
  - `NetKind` (`GraphPetriNet | MatrixPetriNet`): The execution model under test.
  - `TransitionCount` (`int`): Number of transitions represented.
  - `PlaceCount` (`int`): Number of places represented.
  - `HotspotSlice` (`string`): The slice exercised by the scenario.
  - `MetricFocus` (`BytesPerOperation | Latency | Both`): Primary performance signal.
- Validation rules:
  - Scenarios must be deterministic and runnable from source control.
  - At least one graph-path and one matrix-path scenario must exist for changed code.

### Parity Property Family

- Description: A property-based test family that proves one behavioral invariant survives an R2 change.
- Fields:
  - `PropertyName` (`string`): Human-readable invariant name.
  - `GeneratedInputShape` (`string`): Description of generated net and marking inputs.
  - `Invariant` (`string`): The behavior that must remain unchanged.
  - `ObservedDimension` (`Enablement | FiringResult | InvocationOrder | EmptyResult`): The externally visible dimension being protected.
- Validation rules:
  - Each property must describe a behavioral class, not a canned example.
  - Each changed hotspot must be covered by at least one parity property.

## Relationships

- A `Hotspot Slice` contains one or more `Materialization Site` and `Repeated Traversal Site` entries.
- An `Allocation Benchmark Scenario` exercises one `Hotspot Slice`.
- A `Parity Property Family` constrains the allowed behavior of one or more changed hotspots within a slice.
- Successful delivery requires both the benchmark scenario and the parity property family to pass for the same slice.

## State Transitions

1. Select an R2 `Hotspot Slice` from the documented locations.
2. Identify its current `Materialization Site` and `Repeated Traversal Site` behavior.
3. Define the parity properties that preserve observable behavior.
4. Apply the allocation and traversal reduction.
5. Re-run parity tests.
6. Re-run the hotspot benchmark scenario and compare allocation and latency signals against baseline.
7. Accept the slice only if both parity and performance goals are satisfied.