# AppSupportHub

AppSupportHub is an independent portfolio demonstration of an internal
application-support and change-management portal for an enterprise information
technology team. The eventual product will bring application cataloguing,
support work, change assessment, legacy import boundaries, and operational
reporting into one maintainable system.

## Status: Phase 03 — PostgreSQL and Infrastructure

PostgreSQL persistence now implements the Phase 02 repository contracts through
EF Core and Npgsql. Explicit schema constraints, stable append-only WorkItem
history, `citext` names, transactions, migrations, and `xmin` optimistic
concurrency are verified against throwaway PostgreSQL 17 Testcontainers. No
business UI or API exists yet.

AppSupportHub is an independent portfolio project. It is **not affiliated with,
endorsed by, or built for the City of Winnipeg**. It does not use City data or
connect to City systems.

## Technology

- .NET 10 and C# 14
- ASP.NET Core Razor Pages
- PostgreSQL 17, EF Core 10, and Npgsql
- Testcontainers for isolated PostgreSQL integration tests
- xUnit
- Built-in .NET analyzers and health checks

Authentication, external integrations, reporting, and a frontend build pipeline
are not included in Phase 03.

## Solution structure

```text
src/
  AppSupportHub.Domain/          Systems and WorkItems business rules
  AppSupportHub.Application/     Five use cases and persistence ports
  AppSupportHub.Infrastructure/  EF Core PostgreSQL mappings and repositories
  AppSupportHub.Web/             Razor Pages host
tests/
  AppSupportHub.UnitTests/        Domain and Application tests and doubles
  AppSupportHub.IntegrationTests/ Real PostgreSQL persistence tests
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

Run these commands from the repository root:

```bash
dotnet tool restore
dotnet restore AppSupportHub.sln
dotnet build AppSupportHub.sln --no-restore
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
dotnet run --project src/AppSupportHub.Web/AppSupportHub.Web.csproj
```

Integration tests start one isolated `postgres:17-alpine` container and apply
the repository migration. Production data is never seeded. The standard future
runtime configuration key is `ConnectionStrings:AppSupportHub`, with
`ConnectionStrings__AppSupportHub` as its environment override; no real
connection string or password belongs in source control. The current minimal
Web host does not connect to PostgreSQL at startup.

The local host exposes:

- `/` — the Phase 03 project-status page
- `/health` — the built-in liveness health response

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
- [AI-assisted development](docs/ai-assisted-development.md)

## Current limitations

Phase 03 contains no business Web UI or REST API, authentication or
authorization, change assessment, legacy import, reporting, database readiness
check, Dockerfile or Compose configuration, CI/CD, deployment automation, or
production operations configuration. Persistence exists but is not yet exposed
to users. The application is not a production-ready service.

## License

AppSupportHub is available under the [MIT License](LICENSE).
