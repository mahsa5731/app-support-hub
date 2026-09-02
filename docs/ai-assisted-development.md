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

## Phase 04B

Codex implemented thin Systems and WorkItems Razor Pages and a path-versioned
Minimal API over the existing Phase 04A handlers. The work added an
Application-owned case-insensitive string parsing/label boundary, cohesive Web
composition, shared RFC 7807/page error mapping, UTC input handling, the
server-owned demo actor, built-in OpenAPI `v1`, and an explicitly gated,
idempotent fictional Development seeder. No Domain, schema, migration, project
reference, or later-phase security feature was added.

The only packages added were `Microsoft.AspNetCore.OpenApi` 10.0.11 in Web and
`Microsoft.AspNetCore.Mvc.Testing` 10.0.11 in IntegrationTests. Codex selected
feature-local PageModel/endpoint/DTO structure, compact HTTP test arrangements,
Bootstrap-based presentation, and obviously fictional seed names within the
supplied routes, policies, and boundary constraints.

Focused HTTP evidence uses one `WebApplicationFactory<Program>` wired to the
existing shared PostgreSQL 17 Testcontainer. It covers real antiforgery and
Post/Redirect/Get, all API routes, persistence, validation/not-found/conflict/
business errors, server actor ownership, bounded lists and cancellation,
equal-timestamp history order, Development seed gates, rendered semantic and
keyboard-focus evidence, and database-independent liveness. The accessibility
assertions and manual script are basic evidence only, not a manual assistive-
technology audit or WCAG certification.

Final validation uses:

```text
dotnet restore AppSupportHub.sln
dotnet build AppSupportHub.sln --no-restore
dotnet test tests/AppSupportHub.UnitTests/AppSupportHub.UnitTests.csproj --no-build
dotnet test tests/AppSupportHub.IntegrationTests/AppSupportHub.IntegrationTests.csproj --no-build
dotnet test tests/AppSupportHub.ArchitectureTests/AppSupportHub.ArchitectureTests.csproj --no-build
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
git diff --check
```

The final build passed with zero warnings and zero errors. UnitTests passed 226
cases, IntegrationTests passed 49 cases against the shared PostgreSQL 17
container, and ArchitectureTests passed 16 cases, with no skips. The single
full solution run passed all 291 tests. Formatter verification and
`git diff --check` passed.

The final smoke used a separate ephemeral `postgres:17-alpine` container,
explicitly applied the unchanged migration through the Infrastructure
design-time factory, and started the HTTPS host with the synthetic seed enabled.
`/`, `/Systems`, `/WorkItems`, `/health`, both list APIs, and OpenAPI returned
200; OpenAPI exposed 14 operations; three fictional systems, five fictional work
items, and the Phase 04, demo-mode, and non-affiliation statements were visible.
The in-app Browser runtime had no available browser instance, so direct local
HTTPS requests supplied the no-screenshot smoke evidence. Host and container
shut down cleanly. No authentication, authorization, readiness check, Swagger
UI, automatic migration, production seed, CI/CD, deployment, commit, push, tag,
or line-by-line human review is claimed.

## Phase 05

Codex implemented the deliberately lean Phase 05 slices: one structured,
idempotent assessment for an existing ChangeRequest and one strict legacy CSV
preview that validates and labels duplicates without importing, storing, or
mutating records. The earlier import/audit-trail roadmap promise was removed.
CsvHelper 33.1.0 is the only new package and is referenced only by
Infrastructure. AI assistance covered scoped implementation, generated-migration
review, focused tests, documentation, budget measurement, and local validation;
no line-by-line human review is claimed.

Measured handwritten growth is 12 production/view files and 1,045 nonblank
production lines, plus 3 test files, 6 test methods, and 327 nonblank test lines.
The generated `AddChangeAssessments` migration, designer, and snapshot changes
are excluded from that production budget. Documentation growth remains below
the 200-line cap.

Validation performed included:

```text
dotnet restore AppSupportHub.sln
dotnet build AppSupportHub.sln --no-restore
dotnet test tests/AppSupportHub.UnitTests/AppSupportHub.UnitTests.csproj --no-build --filter "FullyQualifiedName~Phase05"
dotnet test tests/AppSupportHub.IntegrationTests/AppSupportHub.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Phase05"
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
dotnet ef migrations has-pending-model-changes --project src/AppSupportHub.Infrastructure/AppSupportHub.Infrastructure.csproj --startup-project src/AppSupportHub.Infrastructure/AppSupportHub.Infrastructure.csproj --no-build
git diff --check
```

The final build passed with zero warnings/errors. Focused tests passed 2 unit
and 4 integration cases; the final solution run passed 228 unit, 53 integration,
and 16 architecture cases (297 total) with zero skips. The first full attempt
exposed two Phase 03 assertions that hard-coded one migration; after correcting
them to expect the required two, their focused check and the final gate passed.
The prompt's Web-startup pending-model command was also attempted, but Web
intentionally lacks EF Design; the existing Infrastructure design-time factory
confirmed no pending changes without adding a second package or weakening the
boundary.

The smoke explicitly migrated an ephemeral PostgreSQL 17 container, used the
existing fictional Development seed, saved/reloaded an assessment, previewed
the sample as one Ready/duplicate/reject row each, proved the system count stayed
three, and received HTTP 200 `Healthy`. No browser session was connected, so
real antiforgery HTTPS requests supplied the interaction evidence. The host,
container, and temporary files were removed. Known limits remain demo identity,
preview-only legacy integration, and no authentication, authorization, API
expansion, upload persistence, import, or production-readiness claim.

## Phase 06

Codex implemented the lean Web-owned security adapter: optional externally
configured Analyst and Administrator accounts, secure cookie login/logout,
named write policies, authenticated mutation actors, API antiforgery, fixed-
window limits, security headers, public read-only rendering, and concise tests
and documentation. The specification fixed the identity model, role matrix,
public-demo boundary, budgets, and exclusions; no package, schema, migration,
persistent identity, or later-phase operations work was added.

Measured growth is 6 production/view files and 576 nonblank production/view
lines, plus 1 test file, 6 test methods, and 350 nonblank test lines.
Documentation growth remains below the 150-line cap.

Validation performed included:

```text
dotnet restore AppSupportHub.sln
dotnet build AppSupportHub.sln --no-restore
dotnet test tests/AppSupportHub.IntegrationTests/AppSupportHub.IntegrationTests.csproj --no-build --filter "Phase=06|FullyQualifiedName~Phase06"
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
dotnet ef migrations has-pending-model-changes --project src/AppSupportHub.Infrastructure/AppSupportHub.Infrastructure.csproj --no-build
git diff --check
```

The build passed with zero warnings/errors; 6 focused security tests and the
final 228 unit, 59 integration, and 16 architecture tests passed (303 total,
zero skips). The first full gate exposed one stale presentation phrase in an
existing test; correcting that assertion required no production or test-line
growth, and the repeated gate passed. Format and model checks passed. One
ephemeral PostgreSQL 17/HTTPS smoke verified public reads, protected writes,
Analyst actor persistence and System denial, Administrator System mutation,
cookie API antiforgery, headers, login limiting, and disabled-login read-only
startup; all temporary processes, container, credentials, and files were
removed. Identity remains a configuration-backed portfolio adapter, and audit
evidence remains intentionally partial; no line-by-line human review is claimed.
