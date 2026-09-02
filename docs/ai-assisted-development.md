# AI-assisted development record

## Principle

AI accelerates implementation, but scope, acceptance, verification, and
presentation remain human responsibilities. Generated work must be inspectable,
tested, and described without overstating authorship or review.

## Phase 01

Codex generated the repository foundation: the classic solution, seven .NET 10
projects, reference graph, central build and package configuration, editor and
Git standards, Razor Pages foundation host, liveness endpoint, assembly markers,
architecture tests, and initial project documentation.

The user-supplied specification decided the product name and location, four-layer
architecture, exact reference graph, .NET and C# versions, Razor Pages and xUnit
choices, central package policy, health endpoint, documentation set, phase
boundaries, non-affiliation language, and explicit exclusions. Codex selected
only conventional implementation details needed to express those decisions.

Validation completed:

```text
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
git --version
dotnet sln AppSupportHub.sln list
dotnet restore AppSupportHub.sln
dotnet build AppSupportHub.sln --no-restore
dotnet test tests/AppSupportHub.ArchitectureTests/AppSupportHub.ArchitectureTests.csproj --no-build
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
dotnet run --project src/AppSupportHub.Web/AppSupportHub.Web.csproj --no-build --launch-profile https
```

Restore passed. The final build passed with zero warnings and zero errors. All
six architecture tests passed, the full solution test command passed with the
same six tests, and formatting verification reported no changes. Local requests
to `/` and `/health` began over HTTP, followed the configured HTTPS redirect,
and returned HTTP 200. The landing page contained the product name, phase label,
and non-affiliation statement; the health response was `Healthy`. The verified
host run shut down cleanly with no exception in its console.

The .NET template engine required its normal per-user template cache during
initial generation. NuGet restore and the test/web processes required approved
network or local-socket access in the execution sandbox. An initial HTTP/2 curl
run exposed a local macOS/.NET TLS flush error after successful responses; the
host was restarted and the required smoke checks were repeated over HTTP/1.1
without an exception. This did not require a code or configuration workaround.
All repository deliverables were created in the specified target. There are no
specification deviations or unresolved Phase 01 issues.

This document must be updated in every later phase with the assistance actually
used, decisions retained by the human owner, validation performed, deviations,
and unresolved issues. No claim is made that generated code has received a
line-by-line human review.

## Phase 02

Codex implemented the Systems and WorkItems Domain modules, encapsulated
lifecycle and workflow invariants, immutable work-item history, specific
Application persistence ports, the structured result model, five explicit use
case handlers, handwritten UnitTests doubles, focused architecture assertions,
and Phase 02 documentation/status updates.

The specification fixed the public state, enums, length limits, exact transition
matrices, history conventions, error codes, use cases, repository interfaces,
TimeProvider boundary, test strategy, and later-phase exclusions. Codex made
only local implementation choices needed to express those decisions.

Validation performed before the final full-suite gate:

```text
dotnet restore AppSupportHub.sln
dotnet build AppSupportHub.sln --no-restore
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
dotnet test tests/AppSupportHub.UnitTests/AppSupportHub.UnitTests.csproj --filter FullyQualifiedName~Domain.Systems
dotnet test tests/AppSupportHub.UnitTests/AppSupportHub.UnitTests.csproj --filter FullyQualifiedName~Domain.WorkItems
dotnet test tests/AppSupportHub.UnitTests/AppSupportHub.UnitTests.csproj --filter FullyQualifiedName~UnitTests.Application
dotnet test tests/AppSupportHub.ArchitectureTests/AppSupportHub.ArchitectureTests.csproj
```

The targeted Systems run passed 39 cases before two final invariant tests were
added. The combined final targeted Domain run passed 146 cases, the targeted
Application run passed 28 cases, and the expanded architecture run passed 8
tests. The final integrated build passed with zero warnings and zero errors.
The unit test suite passed 174 tests, the architecture test suite passed 8
tests, and the full solution passed 182 tests; the IntegrationTests project
remains intentionally empty. Formatting passed after formatter-required
naming-only test renames, and the final diff check passed. No new package,
production dependency, persistence implementation, or Web business workflow
was introduced. A local host smoke test returned `Healthy` from `/health` and
the Phase 02 status page from `/`. There were no deviations or unresolved
issues. No line-by-line human review is claimed.

## Phase 03

Codex implemented EF Core and Npgsql persistence behind the existing specific
repository contracts: explicit mappings and constraints, DbContext unit of
work, repositories, dependency registration, stable append-only history,
`xmin` concurrency, one migration, a shared PostgreSQL Testcontainer fixture,
focused integration tests, expanded architecture enforcement, and Phase 03
documentation/status updates.

The supplied specification fixed PostgreSQL, package versions, schema names and
rules, `citext`, string enums, shadow history sequence, concurrency, repository
contracts, migration and seed policy, test coverage, and later-phase
exclusions. The first attempt correctly stopped when consuming projects
resolved EF Core Relational 10.0.4 through Npgsql while Infrastructure resolved
10.0.11. A continuation explicitly approved a direct central and Infrastructure
reference to `Microsoft.EntityFrameworkCore.Relational` 10.0.11; all consumers
then resolved 10.0.11 and `MSB3277` disappeared.

EF model construction proved that the existing internal
`WorkItemHistoryEntry` constructor parameter `occurredAt` could not bind to
`OccurredAtUtc`. The minimal accommodation renamed only that internal parameter
to `occurredAtUtc`; no public API, setter, behavior, or framework dependency was
added. PostgreSQL history round-trip tests provide regression coverage. EF's
generated migration also required analyzer-only file-scoped-namespace and
static column-array edits; no schema operation was changed.

Validation performed before the final integrated gate included:

```text
dotnet tool restore
dotnet restore AppSupportHub.sln
dotnet build AppSupportHub.sln --no-restore
dotnet test tests/AppSupportHub.IntegrationTests/AppSupportHub.IntegrationTests.csproj --no-build
dotnet test tests/AppSupportHub.UnitTests/AppSupportHub.UnitTests.csproj --no-build
dotnet test tests/AppSupportHub.ArchitectureTests/AppSupportHub.ArchitectureTests.csproj --no-build
dotnet tool run dotnet-ef migrations list --project src/AppSupportHub.Infrastructure/AppSupportHub.Infrastructure.csproj
dotnet tool run dotnet-ef migrations has-pending-model-changes --project src/AppSupportHub.Infrastructure/AppSupportHub.Infrastructure.csproj
dotnet tool run dotnet-ef migrations script --idempotent --project src/AppSupportHub.Infrastructure/AppSupportHub.Infrastructure.csproj --output /tmp/appsupporthub-phase03-idempotent.sql
```

The compatibility build passed with zero warnings and errors. UnitTests passed
174 cases, ArchitectureTests passed 11, and 27 IntegrationTests passed against
one shared `postgres:17-alpine` container with no skips. The single migration
has no pending model changes, and its temporary idempotent script was non-empty
outside the repository. The final full solution passed 212 tests: 174 unit, 11
architecture, and 27 PostgreSQL integration tests. Format verification initially
identified only the configured `Async` test-method suffix and private static
field-prefix rules; the formatter could not batch-fix those naming diagnostics,
so the names were changed mechanically. The affected build and 27 integration
tests passed again, followed by clean format and diff checks.

The normal HTTPS launch profile started without a database connection. HTTP `/`
redirected to HTTPS and returned the Phase 03 AppSupportHub status and
non-affiliation text; `/health` returned HTTP 200 and `Healthy`. The host shut
down cleanly. No secret, production seed, later-phase workflow, commit, push, or
tag was added. No line-by-line human review is claimed.

## Phase 04A

Codex implemented the non-HTTP Systems and WorkItems workflow foundation:
Application-owned filters, read models, query ports and handlers; six explicit
mutation handlers over existing Domain behavior; bounded PostgreSQL
`AsNoTracking` projections; scoped dependency registration; focused unit,
integration, and architecture coverage; ADR 0005; and Phase 04A documentation.

The supplied specification fixed the read fields, filters, limits, ordering,
stable errors, cancellation and `TimeProvider` boundaries, mutation save
semantics, PostgreSQL projection behavior, documentation scope, and Phase 04B
exclusions. Codex selected only feature-local type layout, projection structure,
and focused test arrangements needed to implement those decisions. Domain,
schema, migrations, packages, project references, startup, health, and Web
behavior were unchanged.

Validation performed included:

```text
dotnet build AppSupportHub.sln --no-restore
dotnet test tests/AppSupportHub.UnitTests/AppSupportHub.UnitTests.csproj --no-build
dotnet test tests/AppSupportHub.IntegrationTests/AppSupportHub.IntegrationTests.csproj --no-build
dotnet test tests/AppSupportHub.ArchitectureTests/AppSupportHub.ArchitectureTests.csproj --no-build
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
git diff --check
```

The exact final build passed with zero warnings and zero errors. UnitTests
passed 213 cases, IntegrationTests passed 34 cases against one shared
`postgres:17-alpine` container, and ArchitectureTests passed 13 cases, all with
zero skips. The one full solution run passed all 260 tests. Formatting passed
after correcting one reported import-order issue; the final diff check passed.
The test host and parallel MSBuild workspace required approved local-socket/IPC
access outside the execution sandbox. No Web smoke test was run because Web
behavior is intentionally unchanged in 04A.

Phase 04B Razor Pages and REST API adapters, authentication, authorization,
accessibility evidence, deployment, and all later-phase features remain absent.
No commit, push, tag, secret, production data, or line-by-line human review is
claimed.
