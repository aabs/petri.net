# Feature Specification: Streamed PNML-to-Builder Loading

**Feature Branch**: `004-sax-pnml-loader`  
**Created**: 2026-04-13  
**Status**: Draft  
**Input**: User description: "convert the PNML loader from using a LINQ-based iteration model to using single pass SAX (simple API for XML) system. As the XML parser identifies new elements they should result in calls into the Petri Net builder. If this is not easy with the builder, then it is permitted to alter the interface of the builder to better support the flow of elements as they arise from the PNML model. Constraints: must use the minimum number of allocations and be efficient when creating the model using the builder. SAX parser should support the PNML standard (i.e. its expectations should be based on what it might expect from conformant PNML documents). all relevant tests must be updated to work with the new system. all existing sample PNML tests must continue to pass. New code should make use of modern C# idioms, classes and frameworks and aim for high efficiency and low memory consumption."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Build Nets While Streaming PNML (Priority: P1)

As an engineer loading PNML models, I need the loader to construct the Petri net as XML elements are read so that large models load quickly and with low memory overhead.

**Why this priority**: This is the core capability. Without streaming construction, the loader still pays avoidable parsing and memory costs before model creation can start.

**Independent Test**: Can be tested by loading valid PNML documents of different sizes and confirming that the resulting net structure and markings match expected behavior classes while load memory growth remains bounded to active parse state.

**Acceptance Scenarios**:

1. **Given** a valid PNML document containing one or more nets, **When** the loader processes the document from start to end once, **Then** each net is constructed successfully in document order.
2. **Given** PNML elements for places, transitions, and arcs, **When** each element is encountered, **Then** the loader applies the corresponding builder operation without waiting for a second pass over the document.
3. **Given** two PNML files that are behaviorally equivalent but differ in non-semantic formatting, **When** both are loaded, **Then** they produce equivalent Petri net behavior.
4. **Given** conformant PNML using expected core elements and common metadata wrappers, **When** the loader parses it, **Then** the model is loaded without compatibility regressions.
5. **Given** an arc that references a place or transition that was never defined, **When** loading occurs, **Then** loading fails with a clear error describing the missing identifier.
6. **Given** malformed or incomplete core PNML structure, **When** loading occurs, **Then** loading fails with a deterministic and actionable error.
7. **Given** the existing sample PNML test corpus, **When** tests run after migration, **Then** all sample PNML tests pass.
8. **Given** existing property-based tests relevant to loading and firing semantics, **When** tests run after migration, **Then** behavior invariants continue to hold.
9. **Given** updated tests for streamed loading paths, **When** invalid and valid PNML cases are exercised, **Then** observed outcomes match the specified loader contracts.

---

### Edge Cases

- What happens when a PNML document includes extension elements not required for core net construction? They are ignored unless they invalidate required core structure.
- What happens when identifier definitions and references appear near parser boundaries (for example, large text nodes or interleaved metadata)? Loading still resolves identifiers correctly and deterministically.
- What happens when the document is very large? Loading remains single-pass and avoids unbounded growth in temporary parse data.
- What happens when duplicate place or transition identifiers are encountered? Loading fails clearly and does not produce a partially ambiguous mapping.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST load PNML documents using a single-pass streamed XML read model that processes elements in document order.
- **FR-002**: The loader MUST convert recognized PNML element events directly into net-construction actions during parsing, without requiring a full intermediate document model.
- **FR-003**: The system MUST support the PNML core PT-net subset required to construct places, transitions, arcs, and initial markings from conformant documents.
- **FR-004**: The loader MUST maintain deterministic identifier resolution so arc endpoints and marking targets map to the intended net elements.
- **FR-005**: If the current builder contract cannot support event-by-event construction, controlled breaking changes to the public builder contract are allowed only when accompanied by explicit migration notes and preserving externally observable Petri net behavior after migration.
- **FR-006**: The loader MUST reject invalid core PNML input with clear, actionable failures for missing identifiers, malformed required structure, and duplicate identifiers.
- **FR-007**: The migration MUST reduce temporary allocations during load by removing multi-pass iteration and unnecessary intermediate collections.
- **FR-008**: All existing sample PNML tests MUST continue to pass after migration.
- **FR-009**: All relevant automated tests impacted by the loading flow MUST be updated or extended to validate streamed loading behavior and regression safety.
- **FR-010**: The system MUST define contract-sensitive input, output, invariant, and failure behavior using idiomatic modern C# guard and exception patterns.
- **FR-011**: When a PNML document contains multiple `net` elements, the loader MUST construct and return all nets in document order.

### Key Entities *(include if feature involves data)*

- **PNML Element Stream**: Ordered sequence of parsed XML element and text events representing a PNML document.
- **Net Build Session**: Stateful construction context that receives streamed parse events and applies net-construction operations.
- **Identifier Resolution State**: In-memory mapping state used during load to resolve PNML identifiers for places, transitions, arcs, and markings.
- **Loader Failure Contract**: Structured set of failure conditions and messages for malformed or semantically invalid PNML inputs.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For representative large PNML inputs, median end-to-end load duration improves by at least 25% compared to the pre-migration baseline while preserving behavior.
- **SC-002**: For representative large PNML inputs, per-load temporary memory allocation is reduced by at least 30% compared to the pre-migration baseline.
- **SC-003**: 100% of existing sample PNML tests pass after migration.
- **SC-004**: 100% of relevant existing automated tests for PNML loading and dependent behavior pass after migration.
- **SC-005**: New or updated regression tests verify deterministic failure outcomes for invalid identifier references, duplicate identifiers, and malformed required PNML structure.
- **SC-006**: For parity test scenarios, loaded net behavior remains equivalent to pre-migration behavior for valid conformant PNML documents.
- **SC-007**: For PNML inputs with multiple `net` elements, 100% of nets are returned in source order with no omissions.

When performance is a goal, correctness boundaries apply first: all valid PNML behavior and failure contracts must remain correct before performance gains are counted.

## Assumptions

- The feature scope is limited to PNML loading flow and closely related builder interaction changes required for streamed construction.
- PNML multi-net documents are in scope and the loader returns all nets in document order.
- Existing externally observable net semantics (including resulting markings and firing behavior after load) remain unchanged for valid PNML inputs.
- Public builder API changes are permitted for this feature when required for streaming flow, provided migration guidance accompanies the change.
- Existing sample PNML documents are representative of required conformance coverage for this migration.
- Performance outcomes are validated using repeatable repository benchmarks and test scenarios.
- Non-core PNML extension content remains out of conformance scope for this feature unless already supported by current behavior.
