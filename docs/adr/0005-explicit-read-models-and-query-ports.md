# ADR 0005: Explicit read models and query ports

- **Status:** Accepted
- **Date:** 2026-09-02

## Context

Phase 04 needs bounded catalog, work-queue, and detail reads for later Web and
API adapters. Aggregate repositories are designed for invariant-preserving
mutations and should not become unrestricted reporting surfaces. Exposing
aggregates, EF entities, or `IQueryable` would leak persistence behavior across
the Application boundary and allow outer layers to construct unbounded queries.

## Decision

Application owns specific `IApplicationSystemQueries` and `IWorkItemQueries`
ports, validated filter records, and immutable presentation-neutral read models.
Handlers normalize input, enforce limits from 1 through 100, supply time through
`TimeProvider`, and return the existing structured results.

Infrastructure implements the ports with bounded server-side EF Core
`AsNoTracking` projections. PostgreSQL performs matching, filtering, ordering,
joins, overdue calculation, and limiting. WorkItem history is projected in its
Infrastructure shadow-sequence order. Query methods accept cancellation tokens
and expose no EF type or query composition surface.

## Consequences

Later Razor Pages and API endpoints can remain thin and share stable read
contracts without loading or exposing Domain aggregates. Database work stays
bounded and provider-aware, while Application remains independent of EF Core,
Npgsql, ASP.NET Core, and Infrastructure. Some field lists and projection code
are explicit and may need coordinated changes when a use case genuinely needs
new output.

## Alternatives considered

- **Expose `IQueryable`:** rejected because callers could create unbounded,
  provider-coupled queries outside Infrastructure.
- **Return Domain aggregates:** rejected because read consumers do not need
  mutation behavior and could couple presentation to persistence loading.
- **Use a generic query repository or mapping library:** rejected because it
  would obscure feature-specific filters and add indirection without reducing
  current risk.
