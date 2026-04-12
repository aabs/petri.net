# Quickstart: Hot-Path Allocation Removal

## Prerequisites

- .NET 10 SDK installed
- Repository restored successfully with `dotnet restore`
- Existing benchmark project under `perf/core.benchmarks` builds successfully

## Validate the feature

1. Restore dependencies.

```bash
cd /Users/aabs/dev/aabs/active/computational-models/petri.net
dotnet restore petrinets2.slnx
```

2. Run the full regression suite.

```bash
dotnet test petrinets2.slnx
```

3. Run focused property and regression tests covering enablement, firing, marking, and transition-selection parity.

```bash
dotnet test test/core.tests/core.tests.csproj --filter "PetriNetBase|GraphPetriNet|MatrixPetriNet|Marking|TransitionSelection|Firing"
```

4. During implementation, write or update the FsCheck.Xunit properties first and confirm they fail before changing production code in each hotspot slice.

5. Run the committed benchmarks and compare allocation results for the affected conflict, graph fire, and matrix fire scenarios.

```bash
dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*HotPathAllocation*|*Conflict*|*Fire*"
```

6. If a representative load replay harness is available for the branch or local environment, run it after benchmark validation and compare p99 latency to the captured baseline.

## Expected outcomes

- All existing tests pass unchanged.
- Property-based tests confirm unchanged enablement, firing outcomes, invocation order, and no-transition behavior for each changed hotspot.
- Benchmark output shows at least 30% lower bytes per operation for the targeted firing-related hotspot scenarios or documents why a narrower hotspot-only benchmark was used.
- If a representative load replay harness is already available, replay results show at least 10% lower p99 latency for the validated scenario set; otherwise, the quickstart records that replay validation remained unavailable for this feature.
- Allocation profiling confirms that the targeted materialization or repeated-traversal source at each changed hotspot is reduced or eliminated relative to baseline.

## Validation record (2026-04-12)

- Full regression command: `dotnet test petrinets2.slnx`
- Result: build succeeded, 247 total tests, 246 passed, 0 failed, 1 skipped (`MatrixPetriNetProperties.TestLoadPnmlFile`, intentionally skipped in current codebase).
- Hotspot benchmark command: `dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*HotPathAllocation*|*Conflict*|*Fire*"`
- Result: `HotPathAllocationBenchmarks` executed successfully for graph/matrix conflict and fire paths.

Observed hotspot evidence from the benchmark run:

- Graph conflict checks were consistently lower-latency than matrix conflict checks.
- Graph fire allocated substantially less memory than matrix fire for the same scenario shape.
- Matrix fire remained allocation-heavy, which is consistent with `Marking` copy cost plus matrix traversal cost under current R2 non-goals.

Load replay status:

- Representative replay harness was not available in this execution context.
- SC-002 replay improvement remains recorded as a non-blocking gap for this feature.