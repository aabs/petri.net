# Contract: Scratch Buffer Pooling

## Scope

Defines externally observable behavior and internal contract boundaries for pooled scratch acquisition/release during steady-state planning and firing.

## Surface and Compatibility

- Public APIs and externally visible execution semantics remain unchanged.
- Pooling, retry, and cap tuning are internal implementation details.
- Existing deterministic behavior for identical inputs is preserved.

## Input Contract

- Planning and firing operations accept existing net/marking/selection inputs.
- Null and invalid inputs continue to fail with idiomatic C# argument validation semantics.
- Pool configuration values are validated internally before use.

## Behavioral Contract

1. Planning scratch and firing-delta scratch buffers are reused across steady-state cycles where possible.
2. Primary acquisition path is thread-local; overflow acquisition uses shared fallback.
3. Thread-local retention is bounded by an internal per-thread capacity cap.
4. Shared fallback acquisition uses exactly 5 retries with exponential backoff.
5. Retry policy implementation remains internal.
6. If acquisition remains unavailable after retries, operation fails with explicit exception.
7. Acquisition failure is atomic: no partial marking/planning-state mutation is externally observable.
8. Lease ownership is exclusive and release is exactly-once.
9. Repeated identical inputs preserve baseline-equivalent observable outcomes.

## Failure-Mode Contract

- Double release, use-after-release, and cross-call contamination are prevented by lease validation.
- Shared fallback exhaustion throws explicit acquisition exception after retry budget exhaustion.
- Exception paths return or fault active leases and leave externally observable state unchanged.
- No new public API failure modes are introduced beyond explicit internal acquisition exhaustion behavior.

## Validation Contract

- FsCheck properties prove:
  - baseline parity for planning and firing outputs,
  - determinism under repeated identical inputs,
  - ownership invariants (no leak, no double-return, no contamination),
  - failure atomicity under forced fallback exhaustion.
- BenchmarkDotNet evidence proves:
  - >=20% allocation reduction for targeted steady-state planning/fire workloads,
  - throughput regression no worse than 5% versus baseline,
  - concurrency stress shows zero ownership violations.
