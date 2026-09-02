# Architecture

## Goals

AppSupportHub needs a foundation that is easy to explain, test, and extend while
keeping business decisions independent from web and persistence technology. The
architecture prioritizes maintainability, explicit dependencies, secure future
evolution, and deployment as one coherent application.

## Modular Monolith and Clean Architecture

The system is a Modular Monolith: planned business modules remain explicit, but
they build and deploy together. Clean Architecture dependency direction keeps
enterprise rules and use cases inside the core while frameworks and external
resources remain at the edges.

The four production layers have these responsibilities:

- **Domain:** Enterprise concepts and invariant business rules. It has no
  AppSupportHub project dependency.
- **Application:** Use cases, orchestration, validation boundaries, and ports.
  It depends only on Domain.
- **Infrastructure:** Later persistence and external adapter implementations.
  It depends on Application and Domain.
- **Web:** Razor Pages, HTTP endpoints, composition, and presentation. It
  depends on Application and Infrastructure and never directly on Domain.

```mermaid
flowchart LR
    Web[AppSupportHub.Web] --> Application[AppSupportHub.Application]
    Web --> Infrastructure[AppSupportHub.Infrastructure]
    Infrastructure --> Application
    Infrastructure --> Domain[AppSupportHub.Domain]
    Application --> Domain
```

No other production dependency is permitted. The architecture test project
uses assembly reflection to detect forbidden compiled dependencies and reads
the four production project files to verify their declared references exactly.
Each production assembly exposes a minimal `AssemblyReference` marker so tests
and later composition-oriented tooling can locate it without depending on a
business type.

## Layer rules

Razor Page models translate HTTP input and output and call Application use
cases. They must not contain workflow rules, persistence logic, or domain
decisions. Infrastructure implements technical details selected by Application
contracts; it must not become a second business layer. Keeping business logic
in Domain and Application makes it testable without a web server or database.

## Systems and WorkItems core

Phase 02 implements two feature-oriented Domain modules:

- **Systems:** `ApplicationSystem` owns classification, ownership, criticality,
  lifecycle, vendor, and retirement invariants.
- **WorkItems:** `WorkItem` owns incident, enhancement, and change-request
  details; assignment, priority, due date, status, resolution, overdue behavior;
  and its immutable history sequence.

```mermaid
flowchart LR
    System[ApplicationSystem aggregate]
    WorkItem[WorkItem aggregate]
    History[WorkItemHistoryEntry entity]
    WorkItem -->|references by ApplicationSystemId| System
    WorkItem -->|owns ordered history| History
```

ApplicationSystem and WorkItem are separate aggregate boundaries. WorkItem
stores only the application-system ID, so a work-item mutation cannot silently
change system state. Application handlers coordinate cross-aggregate checks,
such as preventing creation for a retired system. WorkItem alone creates and
appends history entries; exposing a read-only collection prevents callers from
rewriting audit facts.

Application defines specific `IApplicationSystemRepository` and
`IWorkItemRepository` ports because each aggregate needs an intentional query
surface and invariant-aware operations. A generic repository would obscure
those differences and encourage unrestricted persistence access. The five use
cases use explicit sealed handlers with `ExecuteAsync`; MediatR and
reflection-based dispatch would add indirection without a current need.

Handlers obtain time from an injected `TimeProvider`. Domain factories and
mutations receive the instant explicitly and normalize stored values to UTC.
This prevents hidden system-clock calls and keeps tests deterministic.

Expected failures use one structured Application error. Stable business-rule
codes introduced in this phase include
`systems.invalid_lifecycle_transition` and
`work_items.assignment_forbidden`; status matrix failures use the specified
`work_items.invalid_transition` code.

## Feature organization

Later phases organize work by plural feature or module name rather than by
technical dumping grounds. Planned modules are Systems, WorkItems,
ChangeAssessments, LegacyImports, Reporting, Identity, and Operations.

Representative future organization:

```text
Application/
  Systems/
  WorkItems/
  ChangeAssessments/
  LegacyImports/
  Reporting/
  Identity/
  Operations/

Web/Pages/
  Systems/
  WorkItems/
  ChangeAssessments/
  LegacyImports/
  Reporting/
  Identity/
  Operations/
```

Systems and WorkItems now exist in Domain and Application. The remaining module
directories are not created until they contain behavior, so empty feature
shells cannot misrepresent implemented scope.

Namespaces follow project and feature folders. Types and files use descriptive
names, one primary top-level type per file, minimal public APIs, and conventional
.NET terminology. Catch-all folders or types named `Helpers`, `Utils`,
`Common`, `Misc`, `Manager`, or `Base` are prohibited unless a later ADR defines
a narrow, specific responsibility.

## Evolution

Feature boundaries allow teams to reason about modules independently while one
process keeps transactions, operations, local development, and deployment
simple. If measured scale or organizational ownership later justifies service
extraction, explicit module contracts provide seams. Microservices are not a
default destination and are unnecessary for the current portfolio scope.

## Phase 02 boundary

The implemented architecture now contains Systems and WorkItems aggregates,
specific persistence ports, a dependency-free result model, and five explicit
Application handlers in addition to the Phase 01 foundation. Infrastructure has
no repository implementation, and Web has no business pages or endpoints.
There is no database, authentication, change assessment, legacy import,
reporting, or deployment implementation.
