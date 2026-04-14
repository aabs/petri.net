# Quickstart: Dense-to-Sparse Matrix Representation Migration

## Goal

Implement and validate migration of internal `MatrixPetriNet` representation from dense traversal to sparse representation while preserving baseline behavior and public API shape.

## Prerequisites

- .NET 10 SDK installed
- Repository restores and builds locally

## 1. Restore and Build Baseline

```bash
dotnet restore petrinets2.slnx
dotnet build petrinets2.slnx
```

If parser generation tooling blocks full build, validate core/test/perf projects with existing outputs as needed.

## 2. Property-First Workflow (Required)

1. Add failing FsCheck properties for:
   - non-zero-only connectivity enumeration from sparse support
   - weighted token-delta parity for arcs with arbitrary positive weights
   - affected-place-only delta application
   - baseline parity of marking, state-equation outputs, enablement/conflicts, and side-effect order
   - determinism under repeated identical inputs
2. Verify failures occur for intended reasons.
3. Implement minimal sparse-backed internal representation changes while preserving public API compatibility.
4. Refactor while keeping all properties green.

Implementation note: Any sparse strategy is acceptable if FR-001 through FR-015 and SC-001 through SC-007 are satisfied; prefer existing repository dependencies unless evidence requires expansion.

## 3. Execute Test Gates

```bash
dotnet test petrinets2.slnx
```

Fallback when parser build tooling is unavailable:

```bash
dotnet test --no-build petrinets2.slnx
```

## 4. Execute Benchmark Gates

Feature-focused run:

```bash
dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*Matrix*|*StateEquation*|*TransitionSelection*"
```

Targeted sparse-backed kernel run (after benchmark names are added):

```bash
dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*Sparse*|*MatrixFire*|*StateEquation*"
```

Validate:
- Sparse fire throughput >=2.0x baseline
- Sparse state-equation throughput >=2.0x baseline
- Dense regression <=5% for both fire and state-equation workloads
- Allocation non-increasing for sparse workloads (or documented justification)

## 5. Migration Completion Verification

- Confirm external `MatrixPetriNet` API shape is unchanged.
- Confirm parity properties pass for fire and state-equation outputs.
- Confirm no runtime fallback dependency remains in accepted design.

## 6. Deliverables

- Sparse-backed internal representation implementation
- New property-based parity, weighted-arithmetic, and determinism tests
- Updated benchmark scenarios (including state-equation focus) and recorded evidence
- Graduation summary tied to SC-001 through SC-007
