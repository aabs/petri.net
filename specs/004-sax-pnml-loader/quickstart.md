# Quickstart: XmlReader PNML Streaming Loader

## Goal

Design and validate a streamed PNML loader using `System.Xml.XmlReader` and a new conventional fluent `PetriNetBuilder` that supports Graph/Matrix output selection, preserves behavior, and reduces load-time allocations.

## 1. Baseline Build

```bash
dotnet restore petrinets2.slnx
dotnet build petrinets2.slnx
```

## 2. Property-First Workflow (Required)

1. Add failing FsCheck properties for streaming-loader invariants before implementation.
2. Confirm failure reasons are intended.
3. Implement minimal production changes to satisfy properties.
4. Refactor while keeping properties green.

Suggested property focus:
- Graph/Matrix behavioral parity from same PNML input.
- Multi-net order preservation.
- Deterministic failure behavior for missing/duplicate identifiers.
- `PetriNetBuilder` sparse-state to dense-model parity and endpoint/index invariants.

## 3. Conformance Regression with Existing Sample Corpus

Use the same embedded sample corpus used by current loader tests:

- `test/core.tests/standard test nets/*.xml`
- Existing loaders tests: `test/core.tests/TestNewPnmlLoader.cs`, `test/core.tests/TestNewPnmlLoaderMatrix.cs`

Run tests:

```bash
dotnet test petrinets2.slnx
```

If parser-generation build tooling is unavailable locally:

```bash
dotnet test --no-build petrinets2.slnx
```

## 4. Deprecation Validation

- Ensure old `PnmlModelLoader` remains behaviorally unchanged.
- Verify deprecation marker and migration messaging are present and discoverable.

## 5. Builder Validation

- Implement `src/core/builders/PetriNetBuilder.cs` with conventional fluent methods (`With*`, `Adding*`, and terminal build methods).
- Verify builder supports incremental construction with sparse intermediate adjacency state.
- Verify `BuildGraph` and `BuildMatrix` produce correct dense final models with no behavioral drift.

## 6. Benchmark Validation

Add/extend benchmark coverage for old vs new PNML load paths and run:

```bash
dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*Pnml*|*Streaming*"
```

Add/extend builder-focused allocation benchmark coverage and run:

```bash
dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*PetriNetBuilder*|*Construction*"
```

Validate success criteria:
- SC-001: >=25% median load-time improvement on representative large PNML inputs.
- SC-002: >=30% temporary allocation reduction per load.

Additional builder validation:
- New builder path does not regress bytes/op versus compatibility path in construction benchmarks.

## 7. Completion Checklist

- New XmlReader-based loader implemented.
- New `PetriNetBuilder` implemented in `src/core/builders` with conventional fluent API.
- Graph/Matrix selection supported by new loader API.
- Multi-net documents return all nets in source order.
- Existing sample corpus passes against new loader path.
- Old loader marked deprecated and left functionally untouched.
- Benchmarks demonstrate required performance and allocation deltas.

## 8. Latest Validation Snapshot (2026-04-13)

- `dotnet test test/core.tests/core.tests.csproj`: pass (131 passed, 0 failed).
- `dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*PnmlStreamingLoadBenchmarks*" --job short`: pass with benchmark output.
- `dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*PetriNetBuilderBenchmarks*" --job short`: pass with benchmark output.

Key benchmark observations:

- PNML load throughput improved significantly for large scenarios.
	- `Legacy_PnmlModelLoader_Load` (1024x1024): `164,032.4 us`
	- `Streaming_LoadGraph` (1024x1024): `1,894.5 us`
	- `Streaming_LoadMatrix` (1024x1024): `2,125.2 us`
- Builder materialization profile:
	- `BuildGraph` (1024x1024): `301.07 us`, `1,390.95 KB`
	- `BuildMatrix` (1024x1024): `614.51 us`, `1,008.88 KB`

Open performance blocker:

- SC-002 target (>=30% temporary allocation reduction per load) is not yet met for streaming loader in the current benchmark snapshot.
	- Legacy allocation (1024x1024): `2,915.32 KB`
	- Streaming graph allocation (1024x1024): `3,914.59 KB`
	- Streaming matrix allocation (1024x1024): `3,532.52 KB`
