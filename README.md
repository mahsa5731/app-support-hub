# AppSupportHub

AppSupportHub is an independent portfolio demonstration of an internal
application-support and change-management portal for an enterprise information
technology team. The eventual product will bring application cataloguing,
support work, change assessment, legacy import boundaries, and operational
reporting into one maintainable system.

## Status: Phase 01 — Foundation

Phase 01 establishes the solution architecture, engineering standards, minimal
Razor Pages host, liveness health check, and automated architecture-boundary
tests. Business features are planned and are not implemented yet.

AppSupportHub is an independent portfolio project. It is **not affiliated with,
endorsed by, or built for the City of Winnipeg**. It does not use City data or
connect to City systems.

## Technology

- .NET 10 and C# 14
- ASP.NET Core Razor Pages
- xUnit
- Built-in .NET analyzers and health checks

No database, authentication system, external service, or frontend build
pipeline is included in Phase 01.

## Solution structure

```text
src/
  AppSupportHub.Domain/          Enterprise rules in future phases
  AppSupportHub.Application/     Use cases and contracts in future phases
  AppSupportHub.Infrastructure/  External implementations in future phases
  AppSupportHub.Web/             Razor Pages host
tests/
  AppSupportHub.UnitTests/        Future domain and application tests
  AppSupportHub.IntegrationTests/ Future host and infrastructure tests
  AppSupportHub.ArchitectureTests/ Enforced dependency boundaries
```

The production dependency direction is Domain ← Application ← Infrastructure,
with Web depending on Application and Infrastructure but not directly on
Domain. See [Architecture](docs/architecture.md) for the exact graph.

## Local development

Prerequisites:

- Stable .NET SDK `10.0.400` or a compatible .NET 10 feature-band update
- Git

Run these commands from the repository root:

```bash
dotnet restore AppSupportHub.sln
dotnet build AppSupportHub.sln --no-restore
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
dotnet run --project src/AppSupportHub.Web/AppSupportHub.Web.csproj
```

The local host exposes:

- `/` — the Phase 01 foundation page
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
- [AI-assisted development](docs/ai-assisted-development.md)

## Current limitations

Phase 01 contains no domain entities, workflows, persistence, security model,
business API, legacy import, reporting, deployment automation, or production
operations configuration. Documentation describes those capabilities only as
future work. The application is an engineering foundation, not a
production-ready service.

## License

AppSupportHub is available under the [MIT License](LICENSE).
