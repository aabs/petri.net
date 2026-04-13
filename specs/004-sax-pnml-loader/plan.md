# Implementation Plan: Streamed PNML Loader via XmlReader

**Branch**: `004-sax-pnml-loader` | **Date**: 2026-04-13 | **Spec**: `/specs/004-sax-pnml-loader/spec.md`
**Input**: Feature specification from `/specs/004-sax-pnml-loader/spec.md`

## Summary

Introduce a new forward-only streamed PNML loader based on `System.Xml.XmlReader` that constructs nets during parse and can produce both `GraphPetriNet` and `MatrixPetriNet` outputs via a selectable target type. Add a new `src/core/builders/PetriNetBuilder.cs` with conventional fluent builder idioms inspired by fifthlang-style builders (explicit `With*` and `Adding*` methods returning the builder and a terminal `Build*` operation). Use sparse intermediate adjacency state during incremental description and compact dense structures at materialization time to minimize allocations and GC pressure. Keep the existing `PnmlModelLoader` implementation untouched but mark it deprecated. Preserve externally observable behavior for valid core PT-net PNML and validate conformance/regression using the same embedded sample corpus already used by loader tests.

## Technical Context

**Language/Version**: C# 14 on .NET 10 (`net10.0`)  
**Primary Dependencies**: BCL `System.Xml.XmlReader`, core library `MathNet.Numerics`, tests `FsCheck` + `FsCheck.Xunit` + `xUnit`, perf `BenchmarkDotNet`  
**Storage**: N/A (file-based PNML parsing, in-memory net construction)  
**Testing**: Property-based tests with FsCheck/FsCheck.Xunit are the gating validation path; existing xUnit corpus checks are compatibility signals only and do not replace property-based acceptance gates  
**Target Platform**: .NET 10 on Linux/Windows/macOS  
**Project Type**: Monolithic multi-project .NET library (`src/core`, `test/core.tests`, `perf/core.benchmarks`)  
**Performance Goals**: Meet SC-001/SC-002 goals from spec (>=25% load-time improvement and >=30% reduced temporary allocations on representative large PNML inputs)  
**Constraints**: Single-pass parse, low-allocation approach, preserve net semantics, support core PT-net subset, return all nets in source order, maintain compatibility testing over existing corpus, builder API should follow conventional fluent idioms and use sparse intermediate state with low-allocation finalization  
**Scale/Scope**: Affects PNML loader surfaces and related tests/benchmarks in `src/core`, `test/core.tests`, and `perf/core.benchmarks`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Gate

- Pass: Plan targets C# 14 and .NET 10 using BCL `XmlReader`.
- Pass: Correctness and failure contracts are explicit before optimization work (multi-net order, missing identifier behavior, malformed input behavior).
- Pass: Test strategy includes property-based work with FsCheck/FsCheck.Xunit under red-green-refactor discipline.
- Pass: Planned properties focus on behavioral invariants/parity, not single canned examples.
- Pass: New/updated code will use idiomatic guards and exceptions; no new Code Contracts usage is planned.
- Pass: New builder design is explicit, contract-driven, and measurable for memory behavior before optimization claims.

### Post-Phase 1 Re-Check

- Pass: `data-model.md` defines streaming parse session state, ID resolution maps, and state transitions.
- Pass: `contracts/pnml-streaming-loader-contract.md` defines input/output/failure and deprecation behavior boundaries.
- Pass: `contracts/pnml-streaming-loader-contract.md` defines `PetriNetBuilder` fluent contract and sparse-to-dense build behavior.
- Pass: `quickstart.md` captures property-first and sample-corpus regression validation gates before performance claims.
- Pass: No constitution violations identified.

## Project Structure

### Documentation (this feature)

```text
specs/004-sax-pnml-loader/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── pnml-streaming-loader-contract.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── core/
│   ├── PnmlModelLoader.cs                     # existing loader, deprecated but untouched semantically
│   ├── PnmlStreamingModelLoader.cs            # new XmlReader-based loader
│   ├── builders/
│   │   ├── CreatePetriNet.cs                  # existing builder, compatibility path
│   │   └── PetriNetBuilder.cs                 # new conventional fluent builder with sparse intermediate state
│   ├── GraphPetriNet.cs
│   └── MatrixPetriNet.cs
├── arclang/
└── cli/

test/
└── core.tests/
    ├── TestNewPnmlLoader.cs                   # existing embedded PNML corpus tests (graph)
    ├── TestNewPnmlLoaderMatrix.cs             # existing embedded PNML corpus tests (matrix)
    ├── PetriNetBuilderProperties.cs           # new builder invariants, parity, and allocation-aware properties
    ├── PnmlStreamingLoaderProperties.cs       # new property-based invariants/parity tests
    └── PnmlStreamingLoaderConformanceTests.cs # updated/added conformance and failure regression checks

perf/
└── core.benchmarks/
    ├── Program.cs
    ├── PetriNetBuilderBenchmarks.cs
    ├── PetriNetBuilderBenchmarkScenarios.cs
    ├── PnmlLoadBenchmarkScenarios.cs
    └── PnmlStreamingLoadBenchmarks.cs
```

**Structure Decision**: Retain the existing repository layout, add `PetriNetBuilder` in `src/core/builders`, and route the new streamed loader through that builder so incremental parse events map directly to fluent builder operations. Preserve current loader as deprecated and update tests to reuse the embedded sample corpus plus new builder and streaming property coverage.

## Phase 0 Research Plan

1. Evaluate `XmlReader` usage patterns for forward-only parsing with minimal allocations and robust namespace handling for core PT-net PNML.
2. Define `PetriNetBuilder` fluent API shape using conventional `With*` / `Adding*` method idioms and final `Build*` operations.
3. Define sparse intermediate representation for places/transitions/arcs and dense finalization strategy for Graph/Matrix outputs.
4. Define strategy for preserving the old loader unchanged while introducing deprecation attributes/docs and migration path.
5. Define how to re-use existing embedded PNML sample corpus as authoritative conformance regression set.
6. Define benchmark evidence strategy for load-time and allocation comparisons between old and new loader paths and for builder construction pressure.

Output artifact: `research.md`.

## Phase 1 Design Plan

1. Define streaming entities and parse-state transitions in `data-model.md` (net scope, element state, arc buffering/resolution, marking extraction).
2. Define `PetriNetBuilder` entities and sparse-to-dense transitions in `data-model.md`.
3. Define contract for new loader API, new builder fluent surface, multi-net ordering, Graph/Matrix selection, and failure/deprecation semantics in `contracts/pnml-streaming-loader-contract.md`.
4. Define quickstart workflow for property-first tests, conformance regression over existing corpus, and benchmark validation in `quickstart.md`.
5. Run `.specify/scripts/bash/update-agent-context.sh copilot` and capture resulting context update.

Output artifacts: `data-model.md`, `contracts/pnml-streaming-loader-contract.md`, `quickstart.md`, updated agent context.

## Phase 2 Preview (for /speckit.tasks)

- Add failing property-based tests for streamed loader invariants and graph/matrix parity.
- Add failing property-based tests for `PetriNetBuilder` invariants (ID uniqueness, arc endpoint validity, sparse/dense parity).
- Implement `PnmlStreamingModelLoader` using `XmlReader` and forward-only event processing.
- Implement `PetriNetBuilder` with conventional fluent API and sparse adjacency intermediate representation.
- Add Graph/Matrix output selection in new loader API.
- Mark old `PnmlModelLoader` as deprecated without changing existing behavior.
- Update/extend sample-corpus conformance tests to cover new loader path.
- Add benchmark coverage for streamed loader path, builder construction path, and allocation profiles.

## Complexity Tracking

No constitution violations were required for this plan.
