# Data Model: Max-Scan Transition Selection

## Overview

This feature does not introduce persisted data. The relevant data model is the in-memory selection state and the property/benchmark scenarios used to prove correctness before performance.

## Entities

### Enabled Transition Candidate

- Description: A transition ID that is currently enabled under a given marking and eligible for selection.
- Fields:
  - `TransitionId` (`int`): Zero-based identifier of the candidate transition.
  - `Priority` (`int`): Effective priority returned by `GetTransitionPriority(transitionId)`.
  - `EnumerationOrder` (`int`): The position in enabled-transition traversal order; lower values represent earlier discovery.
- Validation rules:
  - `TransitionId` must correspond to an existing transition in the net.
  - `Priority` defaults to `0` when no explicit entry exists in `TransitionPriorities`.

### Transition Selection Result

- Description: The outcome of scanning the enabled candidates.
- Fields:
  - `SelectedTransitionId` (`int?`): The chosen transition ID, or `null` if no transitions are enabled.
  - `SelectedPriority` (`int`): The priority of the chosen transition when a selection exists.
- Validation rules:
  - `SelectedTransitionId` must be one of the enabled candidates when non-null.
  - Result must be `null` when the enabled candidate set is empty.

### Benchmark Scenario

- Description: A reproducible benchmark input used to measure selection cost.
- Fields:
  - `NetKind` (`GraphPetriNet | MatrixPetriNet`): Execution path under test.
  - `TransitionCount` (`int`): Number of transitions in the net, with large-net scenarios starting at 100+.
  - `PriorityDistribution` (`Flat | Skewed | Mixed`): Shape of assigned transition priorities.
  - `EnabledCount` (`int`): Number of transitions enabled for the benchmarked marking.
  - `MarkingShape` (`string`): Human-readable description of the marking used to enable candidates.
- Validation rules:
  - Scenarios must be deterministic and runnable in isolation.
  - At least one graph and one matrix scenario must exist.

### Selection Property Family

- Description: A property-based test family describing one invariant over all generated valid nets and markings relevant to transition selection.
- Fields:
  - `PropertyName` (`string`): Human-readable invariant name such as `HighestPriorityWins` or `FirstSeenTieWins`.
  - `GeneratorShape` (`string`): Description of the generated net and marking shape.
  - `Invariant` (`string`): The behavior that must hold for every generated input.
  - `CounterExampleShape` (`string`): The minimal failing case expected during the red phase.
- Validation rules:
  - Each property must cover a class of behavior, not a single canned example.
  - Each property must fail before production code changes begin.

## Relationships

- A `Marking` determines the set of `Enabled Transition Candidate` instances.
- A scan over the enabled candidates produces one `Transition Selection Result`.
- Each `Benchmark Scenario` defines a net and marking combination that yields a specific enabled-candidate set.
- Each `Selection Property Family` ranges over generated net and marking combinations and constrains the valid `Transition Selection Result`.

## State Transitions

1. Build or load a Petri net.
2. Apply a `Marking`.
3. Enumerate enabled transitions.
4. Scan candidates in enumeration order.
5. Return the highest-priority candidate, preserving first-seen order on ties.
6. Return `null` if no candidates exist.
7. Verify the result across property families before evaluating benchmark outcomes.
