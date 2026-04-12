<!--
Sync Impact Report
Version change: template -> 1.0.0
Modified principles:
- [PRINCIPLE_1_NAME] -> I. C# 14 and .NET 10 First
- [PRINCIPLE_2_NAME] -> II. Correctness Before Performance
- [PRINCIPLE_3_NAME] -> III. Property-Based TDD (NON-NEGOTIABLE)
- [PRINCIPLE_4_NAME] -> IV. Behavioral Properties Over Example Tests
- [PRINCIPLE_5_NAME] -> V. Explicit Contracts in Idiomatic C#
Added sections:
- Engineering Standards
- Delivery Workflow and Quality Gates
Removed sections:
- None
Templates requiring updates:
- ✅ .specify/templates/plan-template.md
- ✅ .specify/templates/spec-template.md
- ✅ .specify/templates/tasks-template.md
- ✅ README.md
- ✅ .github/copilot-instructions.md
Follow-up TODOs:
- None
-->
# petri.net Constitution

## Core Principles

### I. C# 14 and .NET 10 First
All production code, tests, benchmarks, and examples MUST target idiomatic C# 14 on .NET 10 unless an amendment explicitly permits an exception. New work MUST prefer current language and runtime features that improve clarity, safety, and maintainability without obscuring behavior. Deprecated frameworks and patterns MUST NOT be reintroduced as a convenience path.

Rationale: The repository exists to modernize a 2009 codebase while preserving its domain behavior. Locking the project to idiomatic modern C# and .NET keeps that modernization intentional instead of partial.

### II. Correctness Before Performance
Behavioral correctness is the primary engineering objective. Performance work is mandatory only after correctness is preserved and made explicit. Any optimization MUST keep externally observable semantics unchanged unless the spec says otherwise, and any performance claim MUST be supported by repeatable measurement.

Rationale: This codebase models Petri net execution, where subtle semantic drift is more damaging than a slower implementation. Performance matters, but only after correctness is established.

### III. Property-Based TDD (NON-NEGOTIABLE)
All testing MUST be property-based and MUST use FsCheck with FsCheck.Xunit. Work MUST follow strict red-green-refactor discipline: write the property first, observe it fail for the intended reason, implement the minimum change to pass, then refactor while keeping properties green. Code without a preceding failing property is non-compliant.

Rationale: The repository already depends on FsCheck and models behavior-rich state transitions that benefit from quantified, invariant-driven tests rather than narrow examples.

### IV. Behavioral Properties Over Example Tests
Properties MUST describe whole classes of behavior, invariants, or algebraic relationships. Tests MUST NOT disguise ordinary example-based unit checks as properties by using a single canned input or a thin wrapper around a deterministic assertion. When a behavior cannot yet be expressed as a broad property, the work is incomplete until the property abstraction is improved.

Rationale: Property-based testing only delivers its value when it explores the space of valid inputs and states. Narrow examples masquerading as properties undermine the repository's quality bar.

### V. Explicit Contracts in Idiomatic C#
Public and non-trivial internal methods MUST define their contracts explicitly using idiomatic C# constructs such as argument validation, nullable annotations, well-named domain types, guard clauses, XML documentation, and targeted assertions where appropriate. New work MUST NOT depend on the deprecated Code Contracts library. Existing Code Contracts usage may be reduced or replaced when touched, but must not be expanded.

Rationale: The project needs design-by-contract clarity, but the old Code Contracts tooling is no longer the right mechanism for modern .NET. The rule preserves contract discipline while steering implementation toward supported language features.

## Engineering Standards

- Features MUST specify contract-sensitive behavior before implementation, including null behavior, tie-breaking, invariants, and failure modes where relevant.
- Performance-oriented changes MUST include measurable acceptance criteria and reproducible benchmark evidence when performance is part of the feature goal.
- New abstractions MUST justify themselves in terms of correctness, clarity, or measured performance. Convenience layering alone is insufficient.
- API compatibility MUST be preserved unless a feature spec explicitly authorizes a breaking change and includes a migration plan.

## Delivery Workflow and Quality Gates

- Every spec, plan, and task list MUST show how the work satisfies the five core principles.
- Every implementation task list MUST include property-writing tasks before implementation tasks for the same behavior.
- Reviews MUST reject changes that add example-style tests in place of true properties, optimize before preserving behavior, or introduce new Code Contracts dependencies.
- Documentation and agent guidance files that describe project conventions MUST remain aligned with this constitution.

## Governance

This constitution supersedes conflicting local conventions and planning defaults inside this repository. Amendments require: (1) a documented rationale, (2) updates to affected templates and guidance files, and (3) an explicit semantic version increment recorded in this document. Versioning rules are: MAJOR for incompatible governance changes or removals of principles, MINOR for new principles or materially expanded guidance, and PATCH for wording clarifications that do not change repository obligations. Compliance review is required for every spec, plan, tasks file, and code review touching repository standards. Runtime guidance for day-to-day work lives in `.github/copilot-instructions.md` and MUST remain consistent with this constitution.

**Version**: 1.0.0 | **Ratified**: 2026-04-12 | **Last Amended**: 2026-04-12
