# GitHub Copilot Instructions

## Priority Guidelines

When generating code for this repository:

1. Respect the repository constitution first. The authoritative principles are C# 14 and .NET 10 first, correctness before performance, property-based TDD, behavioral properties over example tests, and explicit contracts in idiomatic C#.
2. Prefer consistency with the current codebase over generic .NET best practices.
3. Preserve externally observable Petri net behavior unless the active spec explicitly permits a semantic change.
4. Treat performance work as evidence-driven. Optimize only measured hotspots and keep benchmark and profiling validation reproducible.
5. Do not expand legacy `System.Diagnostics.Contracts` usage even though some older files still use it.

## Technology Versions

Detected from the current repository:

- Runtime: .NET 10 via `net10.0` in project files
- C# level: repository guidance and constitution require idiomatic C# 14 on .NET 10
- Core library dependency: `MathNet.Numerics` 5.0.0 in `src/core/petrinets2.core.csproj`
- Test stack:
	- `xunit` 2.9.2
	- `xunit.runner.visualstudio` 2.8.2
	- `FsCheck` 3.2.0
	- `FsCheck.Xunit` 3.2.0
	- `Microsoft.NET.Test.Sdk` 17.12.0
- Benchmark stack: `BenchmarkDotNet` 0.15.2 in `perf/core.benchmarks/core.benchmarks.csproj`

Never generate code that depends on language, framework, or library features beyond those versions.

## Repository Context

Use these files as primary guidance before inferring patterns from source:

- `.specify/memory/constitution.md`
- `.github/copilot-instructions.md`
- `README.md`
- active spec artifacts under `specs/<feature>/`
- performance analysis in `docs/performance-remediation-plan.md` when working on performance features

This repository does not currently have a separate `.github/copilot/` folder with architecture or standards documents. Use the constitution, README, active specs, and surrounding code as the source of truth.

## Architecture And Structure

Observed structure:

```text
src/
├── core/         # Petri net domain model, builders, firing logic, PNML loader
└── arclang/      # Arc language parser/scanner sources and generated files

test/
└── core.tests/   # Property-based and regression tests

perf/
└── core.benchmarks/  # BenchmarkDotNet performance harness

docs/             # Design notes and remediation plans
specs/            # Spec Kit feature artifacts
```

Architecture style is best described as a monolithic multi-project .NET library repository with a clear separation between core domain code, parser code, tests, benchmarks, and feature-planning artifacts.

## Production Code Patterns

Follow these patterns because they are actually present in the codebase:

- Use file-scoped namespaces such as `namespace petrinets2.core;` and `namespace core.tests;` in modernized files.
- Use guard clauses like `ArgumentNullException.ThrowIfNull(...)` and `ArgumentException.ThrowIfNullOrWhiteSpace(...)` in new or touched code.
- Preserve public APIs and signatures unless an active spec explicitly allows a break.
- Keep domain naming explicit and concrete: `Marking`, `FiringPlan`, `TransitionSelection`, `GraphPetriNet`, `MatrixPetriNet`.
- Favor simple, direct loops in hot paths when performance work is being done. The repository now has explicit precedent for replacing LINQ in measured hotspots.
- Use helper abstractions only when they clarify behavior or support shared semantics across graph and matrix implementations. Do not add convenience layers without a correctness or performance reason.
- Prefer `TryGetValue` over `ContainsKey` plus indexer in new hot-path code when the access pattern warrants it.

## Legacy Modernization Rules

This codebase is actively modernizing older code. Follow these rules when touching legacy files:

- It is acceptable to preserve surrounding style where needed for a focused change.
- It is acceptable to reduce existing Code Contracts usage when a file is touched.
- It is not acceptable to introduce new deprecated Code Contracts usage.
- Do not perform broad style cleanup unrelated to the feature.
- Do not convert older files wholesale just to make them look modern if the change is otherwise unrelated.

There is a real mix today between modern guard clauses and legacy contract usage, especially in `MatrixPetriNet` and parser-related files. New code should follow the modern side of that boundary.

## Performance Guidance

Performance guidance in this repository is specific, not generic:

- Correctness comes first. Performance changes must preserve externally observable semantics.
- Optimize only when a spec or plan identifies measured hotspots.
- Prefer loop-based rewrites and reduced allocation only in proven hot paths.
- Keep benchmark evidence reproducible from source control.
- Use `perf/core.benchmarks` rather than ad hoc scripts for committed benchmark coverage.
- When profiling or benchmarking, distinguish removable iterator/materialization overhead from residual baseline costs that belong to later features.

Do not state or assume broader optimization strategies such as pooling, caching, async refactors, or sparse-kernel redesign unless the active spec explicitly calls for them.

### Performance-Focused Coding Practices For This Repository

When a task is explicitly performance-oriented, apply the following practices where measurement justifies them:

- Prefer explicit `for` and `foreach` loops over LINQ in hot paths.
	- Replace `Where`, `Select`, `Any`, `All`, `Count`, `Intersect`, `Concat`, `Union`, and query comprehensions only when they sit on a measured execution path.
	- Preserve readability outside hotspots; do not perform repo-wide LINQ cleanup.
- Avoid hot-path materialization.
	- Do not introduce `ToArray()` or `ToList()` in steady-state execution paths unless the materialized collection is required semantically.
	- If the caller only needs a threshold result such as “more than one”, prefer early-exit counting rather than building a temporary collection first.
- Reduce repeated enumeration.
	- If the same transition, arc, or marking data is scanned multiple times in one decision or fire step, prefer a single-pass or bounded-pass implementation when behavior is unchanged.
	- Prefer computing on the existing backing collection once instead of re-enumerating `IEnumerable<T>` pipelines.
- Prefer direct collection access in hotspots.
	- Use local references to `List<T>`, arrays, dictionaries, and matrices inside tight loops.
	- Prefer `TryGetValue` over `ContainsKey` followed by indexer lookup in hot code.
	- Avoid returning empty arrays solely to satisfy a hot-path branch if a direct branch or local empty path is clearer and cheaper.
- Reduce helper-call overhead in dense loops.
	- In matrix and firing kernels, inline trivial per-iteration helpers when profiling shows the call overhead matters and the resulting code remains understandable.
	- Avoid layering enumerable helpers on top of already dense nested loops.
- Preserve allocation boundaries deliberately.
	- Treat `Marking` copies, arc-list cloning, and firing-plan arrays as real allocation costs that should be measured explicitly.
	- Do not redesign object ownership or mutability just to remove allocations unless the active feature explicitly authorizes that semantic change.
- Use modern .NET performance primitives only when the spec or measurement supports them.
	- `Span<T>`, `ReadOnlySpan<T>`, `ArrayPool<T>`, `FrozenDictionary`, and SIMD/intrinsics are allowed directions, but they are not default choices for this repository.
	- Introduce them only after benchmark or profiling evidence shows the simpler loop-and-allocation cleanup is insufficient.

### Hotspot Patterns To Prefer

The remediation plan and current codebase point to these recurring performance patterns:

- Conflict and enablement checks:
	- Prefer early-exit scans instead of materializing adjacent enabled transitions just to count them.
	- Prefer single-pass enablement evaluation when the current implementation walks the same candidates repeatedly.
- Graph execution paths:
	- Prefer direct loops over `List<InArc>` and `List<OutArc>` in fire and enablement logic.
	- Avoid stacking LINQ iterators over arc lists when the underlying data is already concrete.
- Matrix execution paths:
	- Prefer explicit loops over matrix rows and columns in hot paths instead of enumerable wrappers plus nested helper calls.
	- Keep dense-loop cleanup separate from later sparse-kernel redesign work.
- Lookup-heavy code:
	- Replace repeated `ContainsKey` plus indexer patterns with `TryGetValue` in hot loops when behavior stays the same.

### Benchmarking And Profiling Expectations

For performance work, Copilot should generate code and validation steps that support the repository's actual evidence model:

- Add or extend BenchmarkDotNet benchmarks in `perf/core.benchmarks`.
- Use `MemoryDiagnoser` for allocation-sensitive changes.
- Add `DisassemblyDiagnoser` only when the active feature needs instruction-level comparison; do not add it by default.
- Prefer benchmark scenarios that are deterministic and checked into source control.
- When a hotspot is changed, expect before-and-after evidence at the hotspot level, not only an aggregate end-to-end claim.
- Use profiler language consistent with the repo's plans: allocation call tree, bytes/op, Gen0 pressure, throughput, p99 latency.

### Performance Changes To Avoid By Default

- Do not introduce pooling, caching, or reuse schemes that complicate ownership without a spec and measurements.
- Do not change public APIs purely for performance unless the active feature explicitly authorizes it.
- Do not trade away deterministic behavior, firing order, or token-update semantics for speed.
- Do not collapse broad correctness checks into micro-optimizations unless parity properties and benchmarks are added alongside the change.

## Testing Guidance

Testing requirements are strict and repository-specific:

- All new testing must be property-based and use FsCheck with FsCheck.Xunit.
- Follow red-green-refactor discipline: write the property first, observe the intended failure, implement the minimum change, then refactor.
- Properties must describe behavioral classes or invariants, not disguised single examples.
- Preserve and extend parity-style tests that compare graph and matrix implementations for equivalent behavior.
- Use deterministic scenario builders for benchmarks and cross-model comparisons when appropriate.

Observed test patterns to follow:

- `[Property]` methods in test classes such as `PetriNetBaseProperties`, `GraphPetriNetFireProperties`, and `TransitionSelectionContractProperties`
- FsCheck inputs like `NonNegativeInt`, `PositiveInt`, `NonNull<int[]>`
- Scenario-builder helpers in dedicated files such as `TransitionSelectionPropertyData.cs`
- Assertions expressed as boolean-returning property bodies rather than example-style xUnit `[Fact]` tests in the new work

## Documentation And Comments

Documentation style is mixed but consistent enough to guide new work:

- Public APIs sometimes use XML documentation when behavior or contracts are non-obvious, especially in older core files.
- Many modernized files use minimal or no comments unless the behavior needs explanation.
- README examples are concise and practical, focused on showing how to build and run nets.
- Docs under `docs/` are prose-heavy design artifacts and are the right place for remediation rationale and performance analysis.

Follow these rules:

- Add XML docs only where the surrounding file already uses them or the contract is genuinely non-obvious.
- Prefer clear naming over explanatory comments.
- Use brief comments for non-obvious intent, not line-by-line narration.

## Benchmark And Command Guidance

Use these repository commands when suggesting validation steps:

- `dotnet restore petrinets2.slnx`
- `dotnet build petrinets2.slnx`
- `dotnet test petrinets2.slnx`
- `dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*TransitionSelection*"`

When working on performance features, extend the benchmark filter and scenarios to match the active feature rather than creating a separate benchmark harness unless the existing project cannot support the scenario.

## Project-Specific Guidance

- Treat `GraphPetriNet` and `MatrixPetriNet` as two implementations that should remain behaviorally aligned unless a spec says otherwise.
- Keep builder-driven and PNML-driven workflows working when modifying core semantics.
- Preserve transition-function invocation order and token-update behavior during firing-related changes.
- Active feature specs under `specs/` are authoritative for feature scope, especially when they narrow broader remediation plans into a smaller change set.
- Performance plans in `docs/performance-remediation-plan.md` are guidance for hotspot identification, not blanket permission for repo-wide rewrites.

## Files To Treat Carefully

- `src/arclang/Parser.cs` and `src/arclang/Scanner.cs` appear to be generated or generator-adjacent; prefer editing grammar or frame sources when appropriate instead of hand-editing generated output without cause.
- `src/core/MatrixPetriNet.cs` contains legacy Code Contracts usage. Do not copy that pattern into new code.
- `README.md` may lag slightly behind the actual folder layout. Verify paths against the filesystem before repeating them.

## What Not To Assume

Do not add guidance that is not clearly evidenced in this repository. In particular, do not assume:

- dependency injection frameworks or service-container patterns
- async or background-processing patterns
- logging frameworks or established logging conventions
- security or authentication architecture
- accessibility-specific frontend guidance
- repository-wide use of example-style unit tests
- versioning or release-note conventions beyond the constitution's semantic versioning of governance documents

## Default Copilot Behavior For This Repository

When generating code here:

- scan adjacent files first
- follow the constitution and the active feature spec
- preserve public behavior
- prefer idiomatic modern C# on .NET 10
- use property-based tests for new test coverage
- keep performance work narrow, measured, and benchmark-backed
- prioritize consistency with the surrounding code over generic external patterns

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->

## Active Technologies
- C# 14 on .NET 10 (`net10.0`) + `MathNet.Numerics` (core), `FsCheck` + `FsCheck.Xunit` + `xUnit` (tests), `BenchmarkDotNet` (perf) (003-reverse-lookup-maps)
- N/A (in-memory dictionaries and PNML file input) (003-reverse-lookup-maps)
- N/A (in-memory markings and incidence structures) (005-redesign-sparse-matrix-kernel)

## Recent Changes
- 003-reverse-lookup-maps: Added C# 14 on .NET 10 (`net10.0`) + `MathNet.Numerics` (core), `FsCheck` + `FsCheck.Xunit` + `xUnit` (tests), `BenchmarkDotNet` (perf)
