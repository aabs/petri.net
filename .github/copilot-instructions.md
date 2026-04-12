# petri.net Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-12

## Active Technologies
- C# / .NET 10 + `MathNet.Numerics` in `src/core`; `xUnit` and `FsCheck` in `test/core.tests`; `BenchmarkDotNet` for the new benchmark projec (001-max-scan-selection)
- C# 14 on .NET 10 + `MathNet.Numerics` in `src/core`; `xUnit`, `FsCheck`, and `FsCheck.Xunit` in `test/core.tests`; `BenchmarkDotNet` in the new benchmark projec (001-max-scan-selection)

- C# / .NET 10
- MathNet.Numerics
- xUnit
- FsCheck
- BenchmarkDotNet

## Project Structure

```text
src/
├── core/
└── arclang/

test/
└── core.tests/

specs/
└── 001-max-scan-selection/
```

## Commands

- `dotnet restore petrinets2.slnx`
- `dotnet test petrinets2.slnx`
- `dotnet run -c Release --project perf/core.benchmarks/core.benchmarks.csproj -- --filter "*TransitionSelection*"`

## Code Style

- Nullable reference types enabled and warnings treated as errors
- Use idiomatic C# 14 on .NET 10 for all new code
- Correctness takes precedence over performance work; measure performance claims
- Tests should be property-based with FsCheck/FsCheck.Xunit and follow red-green-refactor
- Express contracts with guard clauses, nullable annotations, assertions, and docs; do not add new Code Contracts usage
- Preserve public APIs and current behavioral semantics
- Prefer explicit loop-based optimizations only in proven hot paths

## Recent Changes
- 001-max-scan-selection: Added C# 14 on .NET 10 + `MathNet.Numerics` in `src/core`; `xUnit`, `FsCheck`, and `FsCheck.Xunit` in `test/core.tests`; `BenchmarkDotNet` in the new benchmark projec
- 001-max-scan-selection: Added C# / .NET 10 + `MathNet.Numerics` in `src/core`; `xUnit` and `FsCheck` in `test/core.tests`; `BenchmarkDotNet` for the new benchmark projec

- 001-max-scan-selection: Added implementation planning artifacts for max-scan transition selection

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
