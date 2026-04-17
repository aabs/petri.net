# Quickstart: Scratch Buffer Pooling

## Goal

Implement and validate pooled scratch reuse for steady-state planning and firing while preserving externally observable behavior and API compatibility.

## Prerequisites

- .NET 10 SDK installed
- Repository restores and builds locally

## 1. Restore and Build

```bash
dotnet restore petrinets2.slnx
dotnet build petrinets2.slnx
```

## 2. Property-First Workflow (Required)

1. Add failing FsCheck properties for:
   - planning parity with baseline,
   - firing parity with baseline,
   - determinism for repeated identical inputs,
   - lease ownership invariants (no leak/double-return/contamination),
   - failure atomicity when fallback retry budget is exhausted.
2. Confirm failures occur for intended reasons.
3. Implement minimal pooling changes in `src/core` hot paths.
4. Refactor while keeping all properties green.

## 3. Execute Test Gates

```bash
dotnet test petrinets2.slnx
```

Fallback when parser tooling blocks full build:

```bash
dotnet test --no-build petrinets2.slnx
```

## 4. Execute Benchmark Gates

Feature-focused benchmark run:

```bash
dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*HotPathAllocation*|*TransitionSelection*|*Scratch*"
```

Validate against success criteria:

- Targeted steady-state allocation rate reduced by >=20%.
- Throughput regression no worse than 5% versus baseline.
- Determinism and ownership validation remain fully green.

Determinism replay benchmark run (SC-004 evidence):

```bash
dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*ScratchBufferPooling*Determinism*"
```

Concurrency stress benchmark run (SC-005 evidence):

```bash
dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*ScratchBufferPooling*Contention*|*ScratchBufferPooling*Stress*"
```

## 5. Contract and Failure Validation

- Verify shared fallback acquisition uses 5 retries with exponential backoff.
- Verify exhaustion throws explicit exception.
- Verify no externally observable partial mutation on acquisition failure.
- Verify default per-thread retained-capacity cap is 256 KiB per buffer kind per thread.
- Verify internal override bounds enforce 64 KiB minimum and 8 MiB maximum.

## 6. Deliverables

- Pooled scratch implementation for planning and firing hotspots.
- New property-based parity/determinism/ownership/failure-atomicity tests.
- Updated benchmark scenarios and recorded evidence for SC-001 through SC-005.

## 7. Latest Evidence (2026-04-14)

### Regression and Property Gates

- Command: `dotnet test petrinets2.slnx`
- Result: Total 281, Passed 280, Failed 0, Skipped 1.
- Note: The single skipped test is existing PNML-loader coverage (`MatrixPetriNetProperties.TestLoadPnmlFile`) and is unrelated to scratch-buffer pooling behavior.

### Benchmark Snapshot

- Command: `dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*ScratchBufferPooling*" --job Dry`
- Result: Completed successfully; methods discovered: `Graph_Planning_Baseline`, `Graph_Planning_Pooled`, `Graph_Fire_Baseline`, `Graph_Fire_Pooled`, `ReplayDeterminism_Pooled`, `ConcurrencyStress_OwnershipViolations`.
- Determinism evidence (SC-004): `ReplayDeterminism_Pooled` executed across benchmark parameter sets and returned deterministic truth-path runs.
- Concurrency stress evidence (SC-005): `ConcurrencyStress_OwnershipViolations` executed across benchmark parameter sets; capture and inspect returned violation counts in the recorded benchmark artifacts when running non-Dry acceptance benchmarks.
- Performance note: Dry job timings indicate pooled allocation improvements for fire-path benchmarks, while throughput currently regresses versus baseline in this snapshot; full non-Dry benchmark runs are required before claiming SC-002/SC-003 acceptance.
