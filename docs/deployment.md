# Deployment handoff

Phase 08A defines the deployment contract without creating provider resources,
accounts, secrets, or a live URL. Phase 08B will deploy the root `Dockerfile` as
a Render Web Service backed by a Neon managed PostgreSQL database.

## Runtime shape

- Render builds the repository `Dockerfile`; no separate build or start command
  is required.
- The container is a non-root, runtime-only .NET 10 image listening on HTTP port
  `8080`.
- Anonymous read journeys stay public and interactive login stays disabled.
- Web startup never creates a database or applies migrations.

Configure these Render environment keys. Store the connection string as a
secret and never place its value in source, CI, documentation, logs, command
output, or responses:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ConnectionStrings__AppSupportHub=<Render secret value from Neon>
AppSupportHub__SeedDemoData=false
AppSupportHub__Security__EnableInteractiveLogin=false
```

The forwarded-headers switch lets ASP.NET Core respect Render's terminating
proxy. Keep secrets only in provider settings. Do not commit or print them.

## Database and seed responsibility

Phase 08B must apply the repository's two existing migrations from a trusted
local environment against Neon before starting or promoting Web. Do not run EF
tools in the runtime container and do not add automatic migration to startup.

The current fictional seed is Development-only, so a new Production database is
empty. Phase 08B may add only a narrow, explicit, idempotent fictional
Production seed mechanism. It must not reuse `Development`, contain real data,
or run implicitly.

## Health and release checks

`GET /health` is process liveness and must not depend on PostgreSQL.
`GET /health/ready` is readiness and must succeed only when PostgreSQL is
reachable. Configure provider health monitoring deliberately; do not treat
liveness as proof that the database is ready.

After migration and deployment, verify these public anonymous responses without
enabling login:

- `/`, `/Systems`, `/WorkItems`, and `/Operations`
- `/api/v1/systems` and `/api/v1/work-items`
- `/openapi/v1.json`
- `/health` and `/health/ready`

Also verify HTTPS at the public URL, forwarded-protocol handling, safe security
headers, correlation behavior, empty-or-fictional-only data, and denied mutation
attempts. Record the deployed revision and sanitized results; never capture
cookies, connection data, credentials, request bodies, or antiforgery values.

## Phase 08B provider work

After Phase 08A is pushed, the repository owner creates the provider accounts
and resources, authorizes Render to access only this GitHub repository, sets
provider secrets, applies migrations, performs the release checks, and documents
rollback. None of those external changes are part of Phase 08A.
