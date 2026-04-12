# Transition Selection Contract

## Scope

This contract defines the required observable behavior of `GetNextTransitionToFire(Marking m)` for both `GraphPetriNet` and `MatrixPetriNet`.

## Inputs

- A valid `Marking` instance representing token counts for the current net state.
- A Petri net with zero or more transitions and optional explicit priority assignments.

## Outputs

- Returns `int?` identifying the selected transition.
- Returns `null` when no transition is enabled for the supplied marking.

## Behavioral Rules

1. The selected transition must be enabled under the supplied marking.
2. Among enabled transitions, the transition with the highest effective priority must be selected.
3. If multiple enabled transitions share the highest effective priority, the first transition encountered in the net's enabled-transition enumeration order must be selected.
4. A transition with no explicit priority entry must be treated as priority `0`.
5. Calling `GetNextTransitionToFire` must not mutate the supplied `Marking` or modify net structure.

## Compatibility Requirements

- The public method signature must remain `GetNextTransitionToFire(Marking m)`.
- Observable behavior must remain equivalent to the pre-change LINQ-based implementation.
- `MatrixPetriNet.CreateFiringPlan` and `GraphPetriNet.Fire` must continue to behave identically for equivalent inputs.
- Contract expression for this feature must rely on idiomatic C# mechanisms and must not introduce new deprecated Code Contracts usage.

## Verification

- FsCheck.Xunit properties must cover highest-priority selection, equal-priority ties, empty enabled sets, and default-zero priority behavior.
- Each property must describe an invariant over generated inputs rather than a single canned example.
- Microbenchmarks must exist for both graph and matrix implementations and be runnable from source control.
