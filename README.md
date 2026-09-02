# AppSupportHub

AppSupportHub is an independent portfolio demonstration of an internal
application-support and change-management portal for an enterprise information
technology team. The eventual product will bring application cataloguing,
support work, change assessment, legacy preview boundaries, and operational
reporting into one maintainable system.

## Status: Phase 08B prepared — Awaiting controlled live release

Razor Pages and the path-versioned REST API now expose the Systems and WorkItems
Application workflows over PostgreSQL. The UI includes bounded filters,
validated forms, lifecycle and work-item actions, UTC dates, and immutable
history. Phase 06 keeps every read journey public while requiring configured
Analyst or Administrator cookie authentication for mutations. Phase 07 adds a
public bounded Operations overview, PostgreSQL readiness, and correlation-aware
request completion logs without turning the demo into a reporting platform.
Phase 08A adds a non-root .NET runtime container, read-only GitHub Actions
validation, and a Render/Neon deployment handoff. Phase 08B now provides an
explicit one-shot fictional Production seed command, but the owner-controlled
provider release and public URL are not complete.

AppSupportHub is an independent portfolio project. It is **not affiliated with,
endorsed by, or built for the City of Winnipeg**. It does not use City data or
connect to City systems.

## Technology

- .NET 10 and C# 14
- ASP.NET Core Razor Pages
- PostgreSQL 17, EF Core 10, and Npgsql
- CsvHelper 33.1.0 for the Infrastructure CSV adapter
- Testcontainers for isolated PostgreSQL integration tests
- xUnit
- ASP.NET Core cookie authentication, authorization, antiforgery, and rate limiting
- Built-in .NET analyzers, OpenAPI, health checks, and structured logging
- Multi-stage Docker build and GitHub Actions validation

Persistent enterprise identity, real legacy import, general reporting/export,
provider deployment, and a frontend build pipeline remain outside the local
Phase 08B checkpoint.

## Solution structure

```text
src/
  AppSupportHub.Domain/          Systems, WorkItems, and assessment rules
  AppSupportHub.Application/     Explicit workflows, input parsing, and ports
  AppSupportHub.Infrastructure/  EF Core PostgreSQL mappings and repositories
  AppSupportHub.Web/             Razor Pages, Minimal API v1, and composition
tests/
  AppSupportHub.UnitTests/        Domain and Application tests and doubles
  AppSupportHub.IntegrationTests/ Real PostgreSQL persistence and HTTP tests
  AppSupportHub.ArchitectureTests/ Enforced dependency boundaries
```

The production dependency direction is Domain ← Application ← Infrastructure,
with Web depending on Application and Infrastructure but not directly on
Domain. See [Architecture](docs/architecture.md) for the exact graph.

## Local development

Prerequisites:

- Stable .NET SDK `10.0.400` or a compatible .NET 10 feature-band update
- Git
- Docker with a reachable Linux engine for PostgreSQL integration tests

Set the required connection string without storing a password in the repository,
apply the existing migration explicitly, and run from the repository root:

```bash
dotnet tool restore
dotnet restore AppSupportHub.sln
export ConnectionStrings__AppSupportHub='Host=localhost;Port=5432;Database=app_support_hub;Username=app_support_hub;Password=replace-locally'
dotnet ef database update --project src/AppSupportHub.Infrastructure --startup-project src/AppSupportHub.Infrastructure
dotnet build AppSupportHub.sln --no-restore
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
dotnet run --project src/AppSupportHub.Web/AppSupportHub.Web.csproj
```

Integration tests start one isolated `postgres:17-alpine` container and apply
the repository migration. The runtime key is
`ConnectionStrings:AppSupportHub`, with `ConnectionStrings__AppSupportHub` as
its environment override. It is required for the business host; startup never
applies migrations or creates a database. No real connection string or password
belongs in source control.

Optional fictional demo records require both Development and an explicit gate:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export AppSupportHub__SeedDemoData=true
dotnet run --project src/AppSupportHub.Web/AppSupportHub.Web.csproj
```

The gate defaults to false, never runs outside Development, and never migrates
or clears data. See [local development](docs/local-development.md) for complete
macOS/Linux setup and shutdown guidance.

The local host exposes:

- `/` — the project-status page
- `/Systems` and `/WorkItems` — server-rendered workflows
- `/WorkItems/{workItemId}/Assessment` — ChangeRequest assessment form
- `/LegacyImports` — preview-only legacy CSV upload
- `/api/v1/systems` and `/api/v1/work-items` — REST API v1
- `/openapi/v1.json` — OpenAPI document
- `/Account/Login` — optional configured-account login
- `/api/v1/security/antiforgery` — authenticated unsafe-API token support
- `/health` — the built-in liveness health response

CSV previews accept strict UTF-8 `.csv` files no larger than 256 KiB and 100
data rows, with the exact header shown in the downloadable fictional sample.
Previewing never stores the upload or changes application-system records.

Interactive access is disabled by default. To enable it, externally configure
`AppSupportHub__Security__EnableInteractiveLogin=true` plus `Username` and
`Password` values beneath both `Analyst` and `Administrator`; passwords must be
at least 12 characters and never belong in tracked files. Administrators manage
Systems; both roles manage WorkItems, assessments, and CSV previews. The public
live URL is a Phase 08 deliverable, not part of this phase.

The HTTPS launch profile listens on `https://localhost:7130` and redirects the
corresponding local HTTP endpoint to HTTPS. Local port settings can be changed
through standard ASP.NET Core launch configuration.

## Project documentation

- [Requirements](docs/requirements.md)
- [Job alignment](docs/job-alignment.md)
- [Architecture](docs/architecture.md)
- [Phase plan](docs/phase-plan.md)
- [ADR 0001: Modular Monolith and Clean Architecture](docs/adr/0001-modular-monolith-and-clean-architecture.md)
- [ADR 0002: Server-rendered Razor Pages UI](docs/adr/0002-server-rendered-razor-pages-ui.md)
- [ADR 0003: Rich domain model and explicit use cases](docs/adr/0003-rich-domain-model-and-explicit-use-cases.md)
- [ADR 0004: PostgreSQL and EF Core persistence](docs/adr/0004-postgresql-ef-core-persistence.md)
- [ADR 0005: Explicit read models and query ports](docs/adr/0005-explicit-read-models-and-query-ports.md)
- [ADR 0006: Thin Razor Pages and versioned Minimal API](docs/adr/0006-thin-razor-pages-and-versioned-minimal-api.md)
- [REST API v1](docs/api-v1.md)
- [Local development](docs/local-development.md)
- [Manual test script](docs/manual-test-script.md)
- [AI-assisted development](docs/ai-assisted-development.md)
- [Operations runbook](docs/operations-runbook.md)
- [Deployment handoff](docs/deployment.md)

Useful diagnostics are public `/health` liveness, PostgreSQL-aware
`/health/ready`, and the read-only `/Operations` page. A valid GUID supplied as
`X-Correlation-ID` is normalized and returned; otherwise the host creates one.
Phase 08B will create the Render/Neon resources and public deployment URL.

## Current limitations

Phase 06 uses optional configuration-backed portfolio accounts, not a persistent
enterprise identity provider. The project has no actual legacy import,
tamper-resistant centralized audit, general reporting/export, persistent lockout, rate
limiting across multiple instances, production security hardening, Compose configuration,
automated deployment, external monitoring, or production operations configuration. The CSV
boundary only previews validation and duplicates. The basic accessibility
checks are not a WCAG certification. This is not a production-ready service.

## License

AppSupportHub is available under the [MIT License](LICENSE).
