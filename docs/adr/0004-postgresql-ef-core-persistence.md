# ADR 0004: PostgreSQL and EF Core persistence

- **Status:** Accepted
- **Date:** 2026-09-02

## Context

Phase 02 defined rich Systems and WorkItems aggregates plus specific
Application repository and unit-of-work ports. Phase 03 needs durable relational
storage without leaking persistence concerns into those core layers. History
ordering, case-insensitive names, constraints, transactions, and concurrency
must be verified on the same database family intended for the application.

## Decision

Use PostgreSQL through Npgsql and EF Core. Infrastructure owns all Fluent API
configuration and implements the two specific repositories. One scoped
`AppSupportHubDbContext` implements `IUnitOfWork`; no forwarding wrapper or
generic repository is introduced.

Pin EF Core, EF Core Design, and EF Core Relational to 10.0.11, Npgsql EF Core
to 10.0.3, and the repository-local `dotnet-ef` tool to 10.0.11. EF Core
Relational is a direct Infrastructure reference because otherwise consuming Web
and test projects selected Npgsql's minimum 10.0.4 dependency while
Infrastructure selected 10.0.11 through the private Design package, producing
`MSB3277` conflicts.

Map application-system names as `citext`, enums as constrained strings, and UTC
instants as `timestamp with time zone`. Use explicit snake_case tables, columns,
keys, indexes, foreign keys, and checks. Domain-created GUIDs are never generated
by PostgreSQL. Map PostgreSQL `xmin` as the optimistic-concurrency row version
for both aggregate tables.

Map WorkItem's existing `_history` field with field access. An Infrastructure
shadow integer `Sequence` preserves exact append order and is unique per WorkItem.
The DbContext assigns only new sequence values and rejects tracked history
modification or deletion before saving.

Use the explicit `InitialPostgreSqlPersistence` migration rather than
`EnsureCreated`. Test migrations, mappings, repositories, constraints,
transactions, concurrency, and composition against a shared
`postgres:17-alpine` Testcontainer rather than SQLite or EF InMemory.

Do not apply migrations or seed automatically during Web startup. Integration
tests arrange and clean synthetic records explicitly; production has no seed
data. Phase 04 may add an explicit, idempotent, opt-in synthetic demo seeder
through Application handlers after real workflows exist.

## Consequences

Database behavior is reproducible and tested against real PostgreSQL. Core
projects remain framework-independent, and persistence races are protected by
database uniqueness and concurrency checks. Local integration testing requires
a reachable Linux Docker engine and may need to pull the PostgreSQL image.
Deployments and future development environments must apply migrations
explicitly and supply `ConnectionStrings:AppSupportHub` through secret-safe
configuration.

EF materialization required one minimal Domain accommodation: the internal
history constructor parameter now matches `OccurredAtUtc`. This changes no
public API or behavior and adds no EF dependency.

## Alternatives considered

- **EF Core InMemory or SQLite:** rejected because they cannot prove PostgreSQL
  `citext`, constraints, `xmin`, or provider behavior.
- **Generic repository:** rejected because it would broaden and obscure the
  aggregate-specific Application contracts.
- **Automatic startup migration or seed:** rejected because host startup should
  not mutate a database or require PostgreSQL in this phase.
- **Integer enums or public persistence setters:** rejected because readable
  constrained values and encapsulated Domain behavior are more maintainable.
