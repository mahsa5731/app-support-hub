# ADR 0006: Thin Razor Pages and versioned Minimal API

## Status

Accepted

## Context

Systems and WorkItems already have explicit Application handlers, bounded read
queries, stable errors, and PostgreSQL adapters. Phase 04 needs both a
server-rendered workflow and a machine-readable HTTP boundary without copying
business rules or adding a mediator, controllers, or a separate frontend.
Web must not reference Domain even though existing commands contain Domain
enums.

## Decision

Use thin Razor PageModels and path-versioned Minimal API groups under
`/api/v1`. Both adapters call the same scoped Application handlers. Primitive
Web inputs pass through small Application-owned string factories that validate
vocabulary and construct typed commands/queries. Application read models expose
string labels for presentation.

Keep shared Web-owned boundaries for Application-error mapping, explicit UTC
input, server demo identity, dependency composition, and Web response DTOs.
Publish built-in OpenAPI JSON named `v1`; do not add Swagger UI or external API
versioning packages.

## Consequences

Business behavior remains in Domain/Application while HTML and JSON can evolve
independently. Route IDs and the server demo actor are authoritative. API
errors are stable RFC 7807 responses, and Razor forms use antiforgery and PRG.
There are a few explicit adapter classes and mappings, but no reflection scan or
generic CRUD abstraction hides dependencies. Authentication, authorization,
security hardening, and authenticated audit identity remain Phase 06 work.

## Alternatives considered

- Controllers would add another action model without improving the small route
  set.
- A mediator would add indirection over already explicit handlers.
- Binding Web directly to Domain enums would violate the Web dependency rule
  and produce binder-specific failures for invalid vocabulary.
- A separate frontend would add a build and deployment boundary that the
  current form-oriented scope does not need.
