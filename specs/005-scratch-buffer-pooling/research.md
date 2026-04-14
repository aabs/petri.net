# Phase 0 Research: Scratch Buffer Pooling

## Decision 1: Use thread-local pools with shared fallback

- Decision: Primary scratch-buffer reuse uses thread-local pools; when thread-local capacity is insufficient, acquire from a shared fallback pool.
- Rationale: Reduces contention and cross-thread state risk while preserving reuse under burst/concurrent load.
- Alternatives considered:
  - Single global locked pool: higher contention risk in hot paths.
  - Per-net pool only: weaker isolation under reentrant multi-threaded workloads.

## Decision 2: Enforce explicit lease ownership lifecycle

- Decision: All pooled buffer access is lease-based with strict acquire/use/release ownership boundaries.
- Rationale: Prevents cross-call contamination, double-return, and lifetime ambiguity.
- Alternatives considered:
  - Implicit return via finalization/disposal heuristics: too error-prone for deterministic hot paths.
  - Shared mutable scratch object without leasing: violates thread-safety guarantees.

## Decision 3: Cap thread-local retained capacity and overflow to fallback

- Decision: Use a configurable per-thread retained-capacity ceiling and route overflow acquisitions to shared fallback.
- Rationale: Provides memory bounds while preserving local fast-path reuse.
- Alternatives considered:
  - Unbounded thread-local growth: risks long-lived memory inflation.
  - Single-buffer-per-thread only: insufficient for nested/reentrant paths.

## Decision 4: Apply bounded retries with exponential backoff

- Decision: Shared fallback acquisition uses exactly 5 retries with exponential backoff.
- Rationale: Balances transient contention tolerance against deterministic upper-bound failure behavior.
- Alternatives considered:
  - Immediate fail-fast: too brittle under temporary pressure.
  - Unbounded retries: risks unbounded latency and hangs.

## Decision 5: Fail atomically on acquisition exhaustion

- Decision: If acquisition still fails after retry budget, throw explicit exception and guarantee no partial planning/fire state mutation.
- Rationale: Preserves correctness and deterministic failure semantics.
- Alternatives considered:
  - Emergency unpooled allocation: masks pressure and weakens operational guarantees.
  - Silent baseline bypass: obscures behavior and reduces observability.

## Decision 6: Keep configuration internal and public API unchanged

- Decision: Pool-cap and retry tuning remain internal/non-public in this feature scope.
- Rationale: Meets compatibility constraint while retaining controlled implementation flexibility.
- Alternatives considered:
  - Public tuning surface now: introduces API complexity and support burden.
  - Compile-time-only constants: too rigid for benchmark and stress validation.

## Decision 7: Validate through property-first plus benchmark evidence

- Decision: Add FsCheck properties for parity, determinism, ownership invariants, and failure atomicity; pair with BenchmarkDotNet allocation and throughput runs.
- Rationale: Aligns with constitution and ensures optimization remains correctness-preserving.
- Alternatives considered:
  - Example-only tests: insufficient coverage for concurrency and reentrancy risks.
  - Benchmark-only validation: cannot prove semantic parity.
