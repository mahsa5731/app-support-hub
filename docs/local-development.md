# Local development

These macOS/Linux instructions keep credentials in the current shell only.
AppSupportHub never creates or migrates a runtime database automatically.

## Prerequisites

- Stable .NET 10 SDK (`10.0.400` or compatible feature-band update)
- Git
- Docker with a reachable Linux engine
- `curl`; `psql` is useful but optional

From the repository root, restore tools and packages:

```bash
dotnet tool restore
dotnet restore AppSupportHub.sln
```

## Start PostgreSQL and migrate

Choose a password interactively so it is not written into a tracked file or
shell command history:

```bash
read -r -s -p "Local PostgreSQL password: " ASH_DB_PASSWORD
echo
export ASH_DB_PASSWORD
docker run --detach --rm --name appsupporthub-postgres \
  --publish 5432:5432 \
  --env POSTGRES_DB=app_support_hub \
  --env POSTGRES_USER=app_support_hub \
  --env POSTGRES_PASSWORD="$ASH_DB_PASSWORD" \
  postgres:17-alpine
export ConnectionStrings__AppSupportHub="Host=localhost;Port=5432;Database=app_support_hub;Username=app_support_hub;Password=${ASH_DB_PASSWORD}"
dotnet ef database update \
  --project src/AppSupportHub.Infrastructure \
  --startup-project src/AppSupportHub.Infrastructure
```

Wait for PostgreSQL readiness before migration if Docker has just downloaded or
started the image. Infrastructure is the EF tooling startup project because it
owns the design-time factory and Design package; Web intentionally does not.
Both repository migrations are applied explicitly by the developer.

## Run

The fictional seed is off by default:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export AppSupportHub__SeedDemoData=false
dotnet run --project src/AppSupportHub.Web/AppSupportHub.Web.csproj
```

To opt into the three fictional systems and five fictional work items, set the
gate to `true` before starting. It is idempotent and Development-only:

```bash
export AppSupportHub__SeedDemoData=true
dotnet run --project src/AppSupportHub.Web/AppSupportHub.Web.csproj
```

The site is public read-only when interactive login is false. For local mutation
testing, choose fictional usernames and enter both passwords without echoing:

```bash
export AppSupportHub__Security__EnableInteractiveLogin=true
export AppSupportHub__Security__Analyst__Username='<fictional-analyst>'
export AppSupportHub__Security__Administrator__Username='<fictional-administrator>'
read -r -s -p "Analyst password: " AppSupportHub__Security__Analyst__Password; echo
read -r -s -p "Administrator password: " AppSupportHub__Security__Administrator__Password; echo
export AppSupportHub__Security__Analyst__Password AppSupportHub__Security__Administrator__Password
```

Both passwords must be at least 12 characters. Login at `/Account/Login`.

Open the HTTPS URL printed by ASP.NET Core. Useful paths are `/`, `/Systems`,
`/WorkItems`, `/api/v1/systems`, `/openapi/v1.json`, and `/health`. The demo
actor is synthetic only for seeding; interactive mutations use the configured username.

## Validate

```bash
dotnet build AppSupportHub.sln --no-restore
dotnet test tests/AppSupportHub.UnitTests/AppSupportHub.UnitTests.csproj --no-build
dotnet test tests/AppSupportHub.IntegrationTests/AppSupportHub.IntegrationTests.csproj --no-build
dotnet test tests/AppSupportHub.ArchitectureTests/AppSupportHub.ArchitectureTests.csproj --no-build
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
git diff --check
```

Integration tests create and dispose one isolated PostgreSQL 17 Testcontainer;
they do not use the local database above.

## Clean shutdown

Stop the Web host with Ctrl+C, then remove the disposable local database and
clear shell configuration:

```bash
docker stop appsupporthub-postgres
unset ConnectionStrings__AppSupportHub
unset AppSupportHub__SeedDemoData
unset AppSupportHub__Security__EnableInteractiveLogin
unset AppSupportHub__Security__Analyst__Username AppSupportHub__Security__Analyst__Password
unset AppSupportHub__Security__Administrator__Username AppSupportHub__Security__Administrator__Password
unset ASPNETCORE_ENVIRONMENT
unset ASH_DB_PASSWORD
```

Because the example container uses `--rm` and no volume, its database is removed
when stopped. Do not use these deletion semantics for data that must be kept.
