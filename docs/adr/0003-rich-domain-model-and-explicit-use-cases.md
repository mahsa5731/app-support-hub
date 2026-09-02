# ADR 0003: Rich domain model and explicit use cases

## Status

Accepted

## Context

Systems and support work have lifecycle, validation, history, and type-specific
workflow rules that must remain consistent before persistence and Web concerns
arrive. Phase 02 also needs deterministic orchestration without committing the
core to a database or dispatch framework.

## Decision

Use rich `ApplicationSystem` and `WorkItem` entities with private mutation and
explicit behavior methods. WorkItem exclusively controls creation of its
immutable history entries. Use direct `Guid` identifiers for the current scope.

Application exposes specific repositories for Systems and WorkItems, one unit
of work port, a small single-error result model, and one explicit sealed handler
per use case. No generic repository, MediatR, or mapping framework is used.
Handlers obtain the current instant through injected `TimeProvider`; Domain
behavior receives timestamps and normalizes them to UTC.

The stable invalid-operation codes selected by Phase 02 are
`systems.invalid_lifecycle_transition` for a forbidden retirement lifecycle
operation and `work_items.assignment_forbidden` for assignment in a terminal
state. Status-transition failures use `work_items.invalid_transition`.

## Consequences

Invariants are enforced regardless of future UI or persistence paths, and tests
can run without framework infrastructure. Specific ports and handlers make
dependencies and save behavior visible. The model contains more purposeful
methods than an anemic entity design, and direct handlers involve some repeated
orchestration. Direct Guids are less type-safe than wrapper IDs but avoid
premature conversion and persistence complexity.

The result factories live in a narrowly named `ApplicationResultFactory`
instead of static members on the generic result type because the enabled .NET
analyzers reject static API members on generic types. The represented result
remains only `ApplicationResult<T>`; no non-generic result type exists.

## Alternatives considered

- Anemic entities were rejected because callers could bypass lifecycle and
  history invariants.
- A generic repository was rejected because it would hide aggregate-specific
  query intent and invite unrestricted operations.
- A mediator framework was rejected because five handlers do not justify
  reflection-based dispatch or another dependency.
- Strongly typed IDs were deferred because direct Guids are sufficient for the
  current scope and reduce Phase 03 mapping overhead.
