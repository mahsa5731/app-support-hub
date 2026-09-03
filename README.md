# AppSupportHub

AppSupportHub is an independent portfolio project demonstrating a maintainable
application-support and change-management portal. It combines server-rendered
workflows, a versioned REST API, PostgreSQL persistence, explicit operational
boundaries, and a small fictional dataset in one explainable modular monolith.

## Live demo

[Open the live AppSupportHub demo](https://app-support-hub.onrender.com/)

[![CI](https://github.com/mahsa5731/app-support-hub/actions/workflows/ci.yml/badge.svg)](https://github.com/mahsa5731/app-support-hub/actions/workflows/ci.yml)

The public deployment is intentionally read-only: interactive login is disabled
and all displayed application and work-item records are fictional. Render's free
tier may need a short cold start after inactivity.

## Key capabilities

- Application-system inventory with ownership, criticality, vendor, lifecycle,
  retirement, bounded search, and filtering.
- Incident, enhancement, and change-request workflows with assignment, priority,
  due dates, validated transitions, resolution, and immutable history.
- Structured change assessment with risk, impact, acceptance, test, and rollback
  planning.
- Strict, preview-only legacy CSV validation and duplicate detection with no
  import or upload persistence.
- A bounded Operations overview with system, work-item, risk, and overdue signals.
- Path-versioned REST API v1 and OpenAPI JSON using the same Application handlers
  as Razor Pages.
- Optional local configured-account security with role policies, antiforgery,
  rate limiting, secure headers, and authenticated mutation actors.
- Separate liveness and PostgreSQL readiness checks, correlation IDs, and
  secret-safe request-completion logging.

## Architecture

The production dependency direction is Domain ← Application ← Infrastructure,
with Web composing Application and Infrastructure without directly referencing
Domain. Architecture tests enforce both compiled and declared dependencies.

```text
src/
  AppSupportHub.Domain/          Business entities, value objects, and rules
  AppSupportHub.Application/     Use cases, validation, read models, and ports
  AppSupportHub.Infrastructure/  EF Core, PostgreSQL, queries, and CSV adapter
  AppSupportHub.Web/             Razor Pages, Minimal API v1, and composition
tests/
  AppSupportHub.UnitTests/        Domain and Application tests
  AppSupportHub.IntegrationTests/ PostgreSQL persistence and HTTP tests
  AppSupportHub.ArchitectureTests/ Dependency-boundary enforcement
```

See the [architecture guide](docs/architecture.md) and
[architecture decisions](docs/adr/) for details.

## Technology

- .NET 10, C# 14, ASP.NET Core Razor Pages, and Minimal APIs
- PostgreSQL 17, EF Core 10, and Npgsql
- CsvHelper for the bounded legacy-preview adapter
- xUnit and Testcontainers
- OpenAPI, built-in health checks, analyzers, and structured logging
- Multi-stage Docker image, GitHub Actions CI, Render, and Neon

## Local development

Configure and migrate a local PostgreSQL database as described in the
[local-development guide](docs/local-development.md), then run:

```bash
dotnet tool restore
dotnet restore AppSupportHub.sln
dotnet build AppSupportHub.sln --configuration Release --no-restore
dotnet test AppSupportHub.sln --configuration Release --no-build
dotnet run --project src/AppSupportHub.Web/AppSupportHub.Web.csproj
```

Runtime startup never creates or migrates a database. Optional fictional local
data and configured mutation accounts are disabled by default.

## Testing and quality

The verified suite contains 307 tests: 228 unit, 63 PostgreSQL/HTTP integration,
and 16 architecture tests. CI restores tools and packages, performs a warning-free
Release build, verifies formatting, runs the full suite, and builds the Docker
image. These checks are portfolio evidence, not production certification or a
WCAG conformance claim.

## Deployment and operations

The live demo runs as a non-root .NET runtime container on Render backed by Neon
PostgreSQL. Database migrations and the idempotent fictional Production seed are
explicit owner-controlled operations; normal Web startup performs neither.

See the [deployment guide](docs/deployment.md) for the sanitized release contract
and the [operations runbook](docs/operations-runbook.md) for health, diagnostics,
redeploy, and rollback guidance.

## Limitations and independence

The demo uses configuration-backed local portfolio accounts rather than an
enterprise identity provider. It has no actual legacy import, general
reporting/export, centralized tamper-resistant audit, external monitoring,
persistent lockout, multi-instance rate limiting, production security hardening,
or production support commitment. The free hosting tier can introduce cold-start
latency. It is not production-ready.

AppSupportHub is not affiliated with, endorsed by, or built for the City of
Winnipeg. It contains no City, employer, customer, or real-person data and does
not claim PowerBuilder, Classic ASP, Oracle, or PL/SQL implementation experience.

AppSupportHub is available under the [MIT License](LICENSE).
