# Quickstart: Reverse Lookup Maps Feature

## Goal

Implement and validate O(1) name-to-index resolution in builder and PNML loading while preserving observable behavior.

## Prerequisites

- .NET 10 SDK installed
- Repository restored and buildable

## 1. Restore and Baseline

```bash
dotnet restore petrinets2.slnx
dotnet build petrinets2.slnx
```

## 2. Property-First Workflow (Required)

1. Add failing FsCheck properties in `test/core.tests` for reverse-map invariants.
2. Confirm failure is for intended reason.
3. Implement minimal production changes in `src/core/builders/CreatePetriNet.cs` and `src/core/PnmlModelLoader.cs`.
4. Refactor while preserving green properties.

## 3. Run Test Gates

```bash
dotnet test petrinets2.slnx
```

If local parser generation tooling is unavailable for `src/arclang`, run the regression suite from existing build outputs:

```bash
dotnet test --no-build petrinets2.slnx
```

Focus checks:
- New reverse-map invariants (forward <-> reverse consistency)
- Existing graph/matrix parity properties
- Existing PNML loader regression coverage

## 4. Run Benchmark Gates

```bash
dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*Construction*|*Pnml*|*TransitionSelection*"
```

Feature-focused benchmark run:

```bash
dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*ReverseLookup*"
```

Validate:
- Construction throughput improvement target (SC-002)
- PNML load scaling and throughput target (SC-003)
- Allocation non-regression target (SC-006)

## 5. Contract Verification Checklist

- Public builder method signatures unchanged.
- Missing-name behavior is explicit and actionable.
- Reverse maps remain consistent under duplicate additions and arc additions.
- PNML place-id lookups no longer rely on value scans.

## 6. Deliverables

- Updated builder and PNML loader implementation
- New/updated property-based tests
- New/updated benchmarks with reproducible scenarios
- Evidence summary for SC-001 through SC-006

## 7. Evidence Expectations

- SC-002: include construction benchmark medians for graph and matrix reverse-lookup paths at large parameters.
- SC-003: include PNML load and marking benchmark medians for reverse-lookup paths at large parameters.
- SC-006: include bytes/op and Gen0 columns from `ReverseLookupBenchmarks` and compare against baseline snapshots.

## 8. Latest Validation Snapshot (2026-04-13)

- `dotnet build petrinets2.slnx`: failed in `src/arclang` because `coco pnarclang.atg` exited with code 150; `src/core` and `perf/core.benchmarks` built successfully.
- `dotnet test petrinets2.slnx`: blocked by the same `arclang` build failure.
- `dotnet test --no-build petrinets2.slnx`: passed regression suite (223 total, 222 passed, 0 failed, 1 skipped).
- `dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*ReverseLookup*"`: succeeded and produced reverse-lookup benchmark measurements.
