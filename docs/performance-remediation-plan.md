# Petri Net Core Performance Remediation Plan (.NET 10)

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Assumptions and Scope](#assumptions-and-scope)
3. [Performance Risk Map](#performance-risk-map)
4. [Data Handling Review](#data-handling-review)
5. [Allocation and Memory Pressure Review](#allocation-and-memory-pressure-review)
6. [Modern .NET Opportunities Since 2009](#modern-net-opportunities-since-2009)
7. [Prioritized Remediation Action Plan](#prioritized-remediation-action-plan)
8. [Validation and Benchmark Plan](#validation-and-benchmark-plan)
9. [Implementation Risks and Tradeoffs](#implementation-risks-and-tradeoffs)
10. [Decision Matrix](#decision-matrix)
11. [30/60/90-Day Execution Plan](#306090-day-execution-plan)
12. [Spec-Kit Priming Pack](#spec-kit-priming-pack)
13. [Top 10 Most Valuable Changes](#top-10-most-valuable-changes)

---

## Executive Summary
The dominant performance risks in the core engine are repeated transition scans, iterator-heavy LINQ in hot execution paths, expensive name-to-index resolution, and PNML parsing patterns that repeatedly traverse XML trees. For large, state-driven telephony-scale systems, these risks primarily degrade throughput and p99/p999 latency by increasing CPU work per state transition and creating avoidable GC pressure.

The fastest high-confidence gains are:
- Replace sort-based and multi-pass transition selection with single-pass indexed loops.
- Remove hot-path materialization and iterator churn.
- Introduce O(1) reverse maps for place/transition lookups.
- Refactor PNML ingestion to indexed/streamed parsing for large models.

---

## Assumptions and Scope

| Item | Value |
|---|---|
| Repository scope | src/core |
| Runtime target(s) | .NET 10, all platforms |
| Throughput/latency goals | Unknown |
| Workload profile | Petri net core state machine for extremely large state-driven systems (for example telephony) |
| Constraints | None specified |
| Evidence model | Static code analysis + inferred hot paths; runtime traces not yet provided |

### Explicit assumptions
- Transition enablement, conflict detection, and fire operations are dominant steady-state CPU paths.
- PNML load is startup/reload-critical and can become a memory hotspot for very large models.
- Multi-threaded execution and model load may occur in production; thread safety and contention are relevant.

### Fastest validation path
1. Add microbenchmarks around enablement and fire loops.
2. Capture allocation and GC counters under realistic model sizes and transition rates.
3. Confirm top stacks with trace-based profiling before high-complexity rewrites.

---

## Performance Risk Map

| Severity | Area | Evidence (path/line) | Primary impact |
|---|---|---|---|
| Critical | Matrix execution loop | src/core/MatrixPetriNet.cs:252, 282, 290 | latency, throughput, scalability |
| Critical | Base enable/conflict logic | src/core/PetriNetBase.cs:16, 22, 28 | latency, throughput, memory |
| High | Graph execution path | src/core/GraphPetriNet.cs:221, 306, 333 | latency, throughput |
| High | Name/index lookup strategy | src/core/builders/CreatePetriNet.cs:224, 229 | throughput, startup |
| High | PNML loading traversal shape | src/core/PnmlModelLoader.cs:47, 118, 147, 165 | startup, memory, scalability |
| Medium | Marking copy/materialization | src/core/Marking.cs:33, 64; src/core/MatrixPetriNet.cs:292 | memory, latency |
| Medium | Thread safety | src/core/PnmlModelLoader.cs:10, 14 | scalability, correctness |

---

## Data Handling Review

| ID | Current pattern | Recommended change | Why it helps | Impact labels | Estimated impact | Effort | Risk | Validation metric |
|---|---|---|---|---|---|---|---|---|
| DH-1 | Repeated enumerable passes for enabled transition discovery and conflict checks | Use single-pass indexed loops and early exits; compute enabled set once per step | Reduces duplicate work, branch overhead, and iterator/state machine cost | latency, throughput, scalability | 1.3x to 3x faster transition evaluation | M | M | transitions/sec, p99 fire latency |
| DH-2 | Sort-like priority selection through LINQ | Use single-pass max-priority scan over enabled transitions | Removes ordering and allocator overhead in hot path | latency, throughput | 10% to 40% CPU reduction in selection path | S | L | CPU samples in selection methods |
| DH-3 | O(n) value-based index resolution for places/transitions | Maintain immutable reverse maps (name -> id) after build | Converts lookup from O(n) to O(1), improves locality | throughput, startup | 2x to 20x faster net construction lookups | S | L | arcs/sec during build and load |
| DH-4 | PNML parser repeatedly traverses Descendants for joins | Build id-indexed maps once; optionally move to XmlReader streaming for huge files | Avoids repeated tree scans and temporary sequences | startup, memory | 30% to 80% large-model load speedup | M/H | M | model load time, peak RSS, allocations |
| DH-5 | Matrix fire scans place x transition even for sparse models | Iterate non-zero adjacency paths with active transition set | Better cache behavior and lower arithmetic work | throughput, scalability | 2x to 10x on sparse nets | H | M/H | cycles/fire vs arc density |

---

## Allocation and Memory Pressure Review

| ID | Current pattern | Recommended change | Why it helps | Impact labels | Estimated impact | Effort | Risk | Validation metric |
|---|---|---|---|---|---|---|---|---|
| AM-1 | ToList/ToArray in hot execution paths | Remove hot-path materialization; use reusable buffers | Lowers Gen0 churn and allocator pressure | memory, latency, throughput | 20% to 60% allocation reduction in hot path | M | M | allocated bytes/op, gen0/sec |
| AM-2 | Marking cloned each fire | Introduce controlled in-place/update-buffer mode where semantics permit | Reduces array copy bandwidth and allocations | memory, throughput | 10% to 50% lower alloc and memory traffic | M/H | H | bytes/op, cache misses, parity tests |
| AM-3 | LINQ iterators and closures in core loops | Replace with explicit loops in hot methods only | Cuts iterator object/delegate overhead | latency, throughput, memory | 5% to 30% in affected paths | S/M | L | allocation stacks and instruction count |
| AM-4 | Tuple-heavy parse intermediates | Use lightweight structs or direct write to target collections | Avoids transient heap objects | memory, startup | 5% to 20% less parse allocation | S/M | L | load alloc/op |
| AM-5 | Potential repeated dictionary probes (ContainsKey + indexer) | Use TryGetValue and local references in loops | Reduces duplicate hash lookups | latency, throughput | 3% to 15% micro-level gains | S | L | microbench hash lookup hot loops |

---

## Modern .NET Opportunities Since 2009

| Capability | Application in this codebase | Expected gain | Effort | Risk |
|---|---|---|---|---|
| Span and ReadOnlySpan | Internal arc/transition working buffers and parser token/value handling | Lower copying, better stack/local usage | M | M |
| ArrayPool/ObjectPool | Reuse temporary transition lists and per-step scratch arrays | Reduced GC pressure and tail jitter | M | M |
| FrozenDictionary/FrozenSet | Immutable lookup maps after model build/load | Faster lookups and better data locality | S/M | L |
| ValueTask (where async exists) | For future async load/pipeline APIs with sync-complete common path | Less task allocation overhead | M | L/M |
| SIMD/vectorization | Dense marking delta and threshold comparisons | Improved arithmetic throughput | H | M/H |
| Hardware intrinsics | Optional high-end kernels if arithmetic dominates profiles | Maximum CPU efficiency on supported hardware | H | H |
| GC tuning modes | Server GC, latency mode windows, heap hard limits by deployment | Better pause/throughput tradeoff | S/M | M |
| Source generators | Schema-shaped PNML readers and strongly-typed parse paths | Lower startup and parse overhead | H | M |
| EventPipe diagnostics | dotnet-trace/counters/PerfView integrated in perf workflow | High-confidence optimization decisions | S | L |

---

## Prioritized Remediation Action Plan

### Immediate wins (low risk, high impact)
1. Replace sort/LINQ transition selection with max-scan loop (DH-2).
2. Remove materialization in hot checks and firing prep (AM-1, AM-3).
3. Add reverse lookup maps for place/transition names (DH-3).
4. Replace ContainsKey+indexer hot patterns with TryGetValue (AM-5).
5. Make PNML id seed thread-safe with atomic increment.

### Near-term refactors (moderate complexity)
1. Consolidate enablement/conflict detection to single-pass logic per cycle (DH-1).
2. Refactor PNML traversal to pre-indexed maps and direct joins (DH-4, AM-4).
3. Introduce pooled scratch buffers for enabled transitions and deltas (AM-1).

### Strategic architecture changes (higher effort, highest long-term gain)
1. Redesign matrix firing kernel to iterate sparse non-zero arcs (DH-5).
2. Introduce optional in-place marking update mode with strict semantics (AM-2).
3. Evaluate SIMD/intrinsics for dense arithmetic kernels after profiling confirmation.

---

## Validation and Benchmark Plan

| Layer | Tooling | Measure | Threshold (until formal SLOs defined) |
|---|---|---|---|
| Microbench | BenchmarkDotNet + MemoryDiagnoser + DisassemblyDiagnoser | Fire, IsEnabled, conflict detection, loader routines | >=25% throughput gain in top kernels; >=40% bytes/op reduction in top allocation path |
| Runtime counters | dotnet-counters | alloc rate, GC collections, heap size, CPU, threadpool | >=30% allocation-rate reduction under equivalent load |
| Tracing | dotnet-trace + PerfView | top inclusive CPU stacks and allocation call paths | prior top hotspot no longer top-3 inclusive CPU |
| Load test | production-shaped scenario | transitions/sec, p95/p99/p999 latency, startup load time | p99 improves >=20%; throughput improves >=30% |

### Benchmark matrix dimensions
- Places: small/medium/large/very-large.
- Transitions: sparse and dense bands.
- Arc density: low/medium/high.
- Inhibitor ratio: none/low/high.
- Priority distribution: flat/skewed.

### Required profiler signals per recommendation
- CPU: method inclusive/exclusive time and sample count.
- Memory: allocated bytes/op, object type histogram, Gen0/sec.
- GC: pause time %, Gen2 frequency, LOH growth.
- Scalability: speedup curve under thread count increments.

---

## Implementation Risks and Tradeoffs

| Theme | Risk | Tradeoff | Mitigation |
|---|---|---|---|
| Loop-based rewrite from LINQ | Medium | less declarative code style | keep hot/non-hot boundary explicit; preserve readability outside hot path |
| Pooling and in-place updates | High | lifetime and ownership complexity | strict ownership API, clear contracts, fuzz/property tests |
| Sparse kernel redesign | Medium/High | deeper algorithmic change | stage behind feature flag with parity oracle |
| GC tuning | Medium | environment-specific behavior | tune by deployment profile with canary rollout |
| SIMD/intrinsics | High | platform/maintenance complexity | fallback path and perf gate by CPU feature |

---

## Decision Matrix

| ID | Recommendation | Impact | Effort | Risk | Confidence | Suggested order |
|---|---|---|---|---|---|---|
| R1 | ~~Max-scan transition selection~~ | High | Low | Low | High | 1 |
| R2 | ~~Remove hot-path materialization~~ | High | Medium | Medium | High | 2 |
| R3 | ~~Reverse lookup maps~~ | High | Low | Low | High | 3 |
| R4 | ~~PNML indexed parse~~ | Medium/High | Medium/High | Medium | Medium | 4 |
| R5 | Sparse matrix fire kernel | Very high | High | Medium/High | Medium | 5 |
| R6 | Pooled scratch buffers | Medium/High | Medium | Medium | Medium | 6 |
| R7 | Thread-safe id generation | Medium | Low | Low | High | 7 |
| R8 | SIMD/intrinsics pass | High (conditional) | High | High | Low/Medium | 8 |

---

## 30/60/90-Day Execution Plan

### 0-30 days
1. Establish reproducible perf harness and baseline.
2. Deliver R1, R2, R3, R7.
3. Add perf regression gate to CI for selected benchmarks.

### 31-60 days
1. Deliver R4 and R6.
2. Expand load profiles and data-volume coverage.
3. Validate GC behavior under sustained and burst load.

### 61-90 days
1. Deliver R5 behind a feature switch, then graduate by evidence.
2. Evaluate and selectively deliver R8 where profile justifies.
3. Finalize runtime and GC configuration per environment.

---

## Spec-Kit Priming Pack

This section is designed to be directly converted into focused Spec-Kit cycles. Each recommendation has a requirement capsule with implementation boundaries, acceptance criteria, and verification hooks.

### Navigation index

| Cycle | Focus | Recommendation IDs |
|---|---|---|
| Cycle A | Hot-path loop and lookup wins | R1, R2, R3, R7 |
| Cycle B | Load-path and allocation stabilization | R4, R6 |
| Cycle C | Kernel redesign for scalability | R5 |
| Cycle D | Optional hardware acceleration | R8 |

### Requirement template used below
- Problem statement
- Functional requirements
- Non-functional requirements
- Out of scope
- Design constraints
- Acceptance criteria
- Validation plan
- Rollout and guardrails
- Task breakdown seed

---

### R1: Max-scan transition selection: Done

**Problem statement**
Priority transition selection currently uses enumerable ordering semantics in hot execution paths.

**Functional requirements**
- Preserve exact transition priority semantics.
- Return null/empty behavior identical to current implementation when no transitions are enabled.
- Keep public API and observable behavior unchanged.

**Non-functional requirements**
- Reduce CPU per selection call by at least 20% in benchmarked large nets.
- Zero additional allocations per call in steady-state.

**Out of scope**
- No changes to conflict semantics.
- No changes to transition priority model.

**Design constraints**
- No API breaks in public classes.
- Deterministic behavior must match baseline tests.

**Acceptance criteria**
- All existing tests pass.
- Benchmark demonstrates target CPU reduction.
- Allocation profile shows no regression.

**Validation plan**
- Method-level microbench for selection path.
- Trace sample verification in end-to-end load.

**Rollout and guardrails**
- Ship immediately without feature flag if parity is proven.

**Task breakdown seed**
1. Implement max-scan helper in both graph and matrix paths.
2. Replace LINQ ordering call sites.
3. Add parity tests for tie and empty cases.
4. Benchmark and publish before/after.

---

### R2: Remove hot-path materialization and iterator churn: Done

**Problem statement**
Current hot methods allocate arrays/lists and iterators that are avoidable.

**Functional requirements**
- Preserve exact enablement and firing behavior.
- Preserve transition function invocation order.

**Non-functional requirements**
- Reduce bytes/op by at least 30% in firing benchmark.
- Improve p99 latency by at least 10% in load replay.

**Out of scope**
- No semantic changes to token updates.

**Design constraints**
- Keep code maintainable: only convert proven hot methods.

**Acceptance criteria**
- No functional regressions.
- Allocation target achieved in benchmark.

**Validation plan**
- BenchmarkDotNet with MemoryDiagnoser.
- PerfView allocation call tree comparison.

**Rollout and guardrails**
- Controlled via small PRs by method group.

**Task breakdown seed**
1. Remove ToList/ToArray in firing and adjacent checks.
2. Replace hot LINQ with loops.
3. Add benchmark case for each changed method.

---

### R3: Reverse lookup maps for names to indices: Done

**Problem statement**
Name-to-index resolution scans dictionaries by value, creating O(n) lookup overhead.

**Functional requirements**
- Add and maintain reverse maps for places/transitions.
- Keep existing external builder API intact.

**Non-functional requirements**
- O(1) expected lookup complexity.
- No measurable startup regression in small models.

**Out of scope**
- No renaming feature redesign.

**Design constraints**
- Reverse maps must stay consistent with source maps.

**Acceptance criteria**
- Existing behavior preserved.
- Build/load benchmark improves by at least 25% for large net construction.

**Validation plan**
- Construction microbench and large model load replay.

**Rollout and guardrails**
- Immediate rollout after consistency tests.

**Task breakdown seed**
1. Introduce reverse map fields.
2. Update add/remove mutation points.
3. Replace value-scan call sites.
4. Add invariant tests.

---

### R4: PNML indexed parsing :Done(Alternate Solution)

**Problem statement**
Repeated Descendants/Where/Single traversals increase parse cost and allocation volume.

**Functional requirements**
- Preserve PNML semantics and current accepted model shapes.
- Keep support for current test fixtures.

**Non-functional requirements**
- Improve large-model load time by at least 30%.
- Reduce load allocations by at least 25%.

**Out of scope**
- No schema expansion unless required by existing fixtures.

**Design constraints**
- Maintain compatibility with current loader APIs.

**Acceptance criteria**
- All PNML tests pass.
- Performance targets met for large fixtures.

**Validation plan**
- Synthetic large PNML benchmark + existing corpus replay.
- Allocation and RSS tracking.

**Rollout and guardrails**
- Feature switch if parser strategy is significantly changed.

**Task breakdown seed**
1. Pre-index XML elements by id and type.
2. Replace repeated descendant traversals with map lookups.
3. Reduce tuple intermediates.
4. Add benchmark corpus and guard thresholds.

---

### R5: Sparse matrix fire kernel redesign: Done

**Problem statement**
The fix must make matrix firing operate on known non-zero structure instead of scanning the full place x transition space.
Concretely, the fire path must:
- Enumerate only entries that are known to exist (non-zero pre/post incidence or equivalent sparse adjacency metadata).
- Skip rows/columns that have no connectivity for the active transition set.
- Stop enumerating paths that are known not to exist in the matrix (zero-valued/non-existent arcs).
- Apply token deltas only for affected places, rather than probing every place for every candidate transition.

**Why this is beneficial**
- Sparse nets have far fewer real arcs than total matrix cells; avoiding full-grid scans reduces per-fire work from grid-size dependent traversal toward work proportional to actual connectivity.
- Eliminating guaranteed-empty probes reduces branch checks and repeated memory reads, improving cache locality and lowering CPU cycles/op.
- Restricting updates to affected places decreases unnecessary arithmetic and dictionary/array access on untouched state.
- Lower steady-state instruction count in the hot fire loop improves throughput and reduces tail latency variability under load.

**Requirements (Spec-Kit ready)**
- The matrix implementation SHALL produce the same post-fire marking as the current baseline for identical net definitions, markings, and selected transitions.
- The matrix implementation SHALL preserve transition function invocation side effects and invocation order exactly as in the baseline implementation.
- The matrix implementation SHALL preserve enablement and conflict behavior parity with baseline behavior for identical inputs.
- The change SHALL be internal to execution logic and SHALL NOT alter public API signatures or externally visible model shape.

**Quality and performance requirements**
- Sparse-profile throughput SHALL improve by at least 2.0x relative to the baseline in the benchmark matrix defined for this feature.
- Dense-profile throughput regression SHALL be no worse than 5% relative to the baseline.
- Allocation rate in sparse fire workloads SHOULD be non-increasing relative to baseline; any increase MUST be justified by benchmark evidence.
- Execution SHALL remain deterministic for identical inputs.

**Out of scope**
- No semantic changes to firing rules, token accounting, or transition side-effect contracts.
- No SIMD/intrinsics-specific optimization work (covered separately by R8).
- No pooling or buffer-lifetime redesign work beyond what is strictly required for this kernel change (covered separately by R6).

**Acceptance criteria**
- Property-based parity tests pass for randomized sparse and dense model families, comparing baseline and redesigned kernels.
- Existing correctness suite passes without behavioral regressions.
- Benchmarks demonstrate >=2.0x sparse throughput gain and <=5% dense regression.
- Determinism checks pass across repeated runs with identical inputs.

**Validation plan**
- Run density-sweep benchmarks (low/medium/high arc density) across fixed place/transition scales.
- Use a differential oracle test harness that executes both baseline and redesigned matrix fire paths over the same generated scenarios.
- Capture throughput and allocation evidence using BenchmarkDotNet and MemoryDiagnoser.

**Rollout and guardrails**
- Ship behind a feature flag that defaults to baseline behavior.
- Promote to default only after parity and performance thresholds are met in CI and representative workload replay.
- Provide immediate fallback to baseline kernel via configuration if regression is detected.

**Task breakdown seed**
1. Define baseline parity oracle and sparse/dense benchmark profiles for the matrix fire path.
2. Implement sparse-fire execution path behind a feature flag, keeping baseline path intact.
3. Add property-based parity and determinism tests over randomized model families.
4. Benchmark sparse and dense profiles, compare against thresholds, and document evidence for graduation.

---

### R6: Pooling for scratch buffers

**Problem statement**
Frequent transient buffer allocations increase GC pressure under sustained load.

**Functional requirements**
- Introduce pooled buffer acquisition/release for hot path scratch data.
- Preserve thread safety and reentrancy assumptions.

**Non-functional requirements**
- Reduce allocation rate by at least 20% under load.
- Avoid buffer leak and double-return defects.

**Out of scope**
- No broad object pooling beyond proven hotspots.

**Design constraints**
- Buffer lifetime ownership must be explicit.

**Acceptance criteria**
- No correctness regressions.
- Allocation target and latency improvement observed.

**Validation plan**
- Stress tests with high concurrency.
- Counters for allocation rate and GC pauses.

**Rollout and guardrails**
- Guard with debug assertions and disposal discipline.

**Task breakdown seed**
1. Introduce pooling utility wrappers.
2. Apply to one hot path first.
3. Add stress/load verification.
4. Expand only after measured win.

---

### R7: Thread-safe id generation

**Problem statement**
Static mutable seed increments are not atomic.

**Functional requirements**
- Ensure unique id generation under concurrency.

**Non-functional requirements**
- No measurable performance regression.

**Out of scope**
- No global identity redesign.

**Design constraints**
- Keep method signatures unchanged.

**Acceptance criteria**
- Concurrent test confirms no collisions.

**Validation plan**
- Multi-threaded stress harness.

**Rollout and guardrails**
- Immediate rollout.

**Task breakdown seed**
1. Replace increment with atomic operation.
2. Add concurrency regression test.

---

### R8: Optional SIMD/intrinsics acceleration

**Problem statement**
Dense arithmetic kernels may be underutilizing vector hardware.

**Functional requirements**
- Add optional vectorized fast path for proven hot arithmetic loops.
- Preserve exact semantics and fallback behavior.

**Non-functional requirements**
- Improve dense-kernel throughput by at least 15% where enabled.

**Out of scope**
- No mandatory hardware-specific path.

**Design constraints**
- Runtime feature detection and fallback required.

**Acceptance criteria**
- Functional parity and measurable gain on supported CPUs.

**Validation plan**
- ISA-specific benchmark suite and cross-platform checks.

**Rollout and guardrails**
- Feature-flagged, environment-scoped rollout.

**Task breakdown seed**
1. Identify dense arithmetic hotspots with profiler.
2. Implement vectorized kernel behind feature check.
3. Add parity tests and perf gates.

---

## Top 10 Most Valuable Changes
1. Max-scan transition selection in hot paths.
2. Remove hot-path ToList/ToArray and LINQ iterators.
3. Add reverse name->index maps.
4. Refactor PNML loader to indexed joins.
5. Redesign sparse matrix fire traversal.
6. Add pooled scratch buffers.
7. Make id generation atomic and concurrency-safe.
8. Introduce immutable frozen lookup collections post-build.
9. Add mandatory benchmark/profiler gates in CI.
10. Add optional SIMD/intrinsics for dense kernels after profiler proof.
