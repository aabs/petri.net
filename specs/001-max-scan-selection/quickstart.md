# Quickstart: Max-Scan Transition Selection

## Prerequisites

- .NET 10 SDK installed
- Repository restored successfully with `dotnet restore`

## Validate the feature

1. Restore dependencies.

```bash
cd /Users/aabs/dev/aabs/active/computational-models/petri.net
dotnet restore petrinets2.slnx
```

2. Run the regression test suite.

```bash
dotnet test petrinets2.slnx
```

3. Run focused tests for transition selection and shared dispatcher parity once they exist.

```bash
dotnet test test/core.tests/core.tests.csproj --filter "GetNextTransitionToFire|TransitionSelection|FiringPlan|Dispatcher|Priority"
```

4. During implementation, write the FsCheck.Xunit properties first and confirm they fail before changing production code.

5. Run the committed microbenchmarks for both graph and matrix paths once the benchmark project exists.

```bash
dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*TransitionSelection*"
```

## Expected outcomes

- All existing tests pass unchanged.
- FsCheck.Xunit properties covering highest-priority, tie, null, firing-plan equivalence, dispatcher ordering, and default-priority behavior pass for both `GraphPetriNet` and `MatrixPetriNet`.
- Benchmark output shows at least 20% lower CPU per selection call and no allocation regression.
