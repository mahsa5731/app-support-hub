# ADR 0001: Modular Monolith and Clean Architecture

## Status

Accepted

## Context

AppSupportHub is a portfolio-scale application-support portal with related
workflows, reporting, identity, and integration concerns. It needs visible
module boundaries and independently testable business rules, but it does not
need distributed deployment, network contracts, or separate operational teams.

## Decision

Build a Modular Monolith with four production projects: Domain, Application,
Infrastructure, and Web. Dependencies point inward according to Clean
Architecture. Project references and reflection-based architecture tests enforce
the permitted graph; the test suite also checks exact project-reference
declarations.

## Consequences

Business rules can evolve independently from UI and persistence choices. One
deployment unit keeps transactions and operations straightforward. The solution
has more projects and explicit dependency discipline than a single-project app,
and later features must respect module ownership rather than share arbitrary
implementation details.

Microservices would add failure modes, versioned network contracts, deployment
coordination, observability overhead, and distributed data decisions without a
measured scaling or team-ownership need. They are inappropriate for the current
scope.

## Alternatives considered

- A single web project was rejected because framework and business concerns
  would be too easy to mix.
- Microservices were rejected because their operational cost is not justified.
- A framework-heavy CQRS architecture was rejected because Phase 01 needs clear
  boundaries, not speculative abstractions or third-party mediation.
