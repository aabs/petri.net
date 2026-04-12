# Research: Hot-Path Allocation Removal

## Decision 1: Restrict R2 work to the documented hotspot locations

- Decision: Limit the feature to the hotspot locations already identified in the remediation plan: `PetriNetBase` enablement/conflict helpers, `GraphPetriNet` enablement and firing paths, `MatrixPetriNet` firing and related iteration paths, and `Marking` materialization points.
- Rationale: The spec now makes scope explicit. R2 is about hot-path materialization and iterator churn, not about unrelated optimizations such as reverse lookup maps, PNML parsing redesign, sparse matrix kernels, or in-place marking semantics.
- Alternatives considered: Bundle R2 with R3 or R5; do a repo-wide LINQ cleanup; add in-place marking updates now. These were rejected because they increase semantic surface area and weaken the evidence trail for this specific remediation step.

## Decision 2: Remove allocations and repeated passes only where evidence already exists

- Decision: Target avoidable `ToArray`, `ToList`, repeated enumerable passes, iterator-heavy filtering, and equivalent transient collection creation only in measured hotspot paths.
- Rationale: The remediation analysis already identifies the classes and approximate line locations where this cost likely occurs. Staying evidence-led avoids turning a performance feature into a style rewrite.
- Alternatives considered: Blanket replacement of LINQ with loops; pre-emptive pooling everywhere; global helper abstractions for every enumeration path. These were rejected because they add maintenance cost without proving user-facing value.

## Decision 3: Treat semantic parity as a cross-cutting gate, not as a feature slice

- Decision: Enforce exact enablement results, firing outcomes, token updates, and transition-function invocation order through requirements, contracts, and property-based verification rather than through a standalone user story.
- Rationale: Semantic preservation is not the feature outcome; it is the boundary that all acceptable implementations must stay within.
- Alternatives considered: Keep semantic parity as a top-level user scenario; rely on existing tests only; validate behavior informally while chasing allocation wins. These were rejected because they blur feature value with engineering discipline or leave correctness under-specified.

## Decision 4: Reuse existing benchmark infrastructure and extend it with hotspot-focused R2 cases

- Decision: Use the committed `perf/core.benchmarks` project as the reproducible benchmark harness for R2 and add or extend scenarios that exercise the targeted firing, enablement, and marking paths.
- Rationale: The repository already contains BenchmarkDotNet infrastructure and transition-selection benchmark scenarios. Extending the existing harness keeps performance validation reproducible and visible from source control.
- Alternatives considered: Create a second perf project; use ad hoc local scripts; rely only on profiler traces. These were rejected because they either fragment the perf workflow or fail the spec's reproducibility goal.

## Decision 5: Preserve existing public APIs and avoid new deprecated contracts usage

- Decision: Keep public API signatures stable and avoid introducing new `System.Diagnostics.Contracts` usage while working around the existing legacy contract surface.
- Rationale: The repository guidelines require public API preservation and discourage further reliance on deprecated Code Contracts. R2 should optimize internals without broad contract-system churn.
- Alternatives considered: Rewrite existing contracts as part of this feature; widen APIs to expose lower-level buffers; add new contract expressions to every hotspot. These were rejected because they are orthogonal changes that would obscure the core performance goal.