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
