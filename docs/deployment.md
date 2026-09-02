# Deployment handoff

Phase 08B is prepared for an owner-controlled deployment of the root
`Dockerfile` to Render Web Service with Neon managed PostgreSQL. The local code
checkpoint creates no provider resource, handles no provider secret, and does
not yet have a live URL.

## Runtime shape

- Render builds the repository `Dockerfile`; no separate build or start command
  is required.
- The container is a non-root, runtime-only .NET 10 image listening on HTTP port
  `8080`.
- Anonymous read journeys stay public and interactive login stays disabled.
- Web startup never creates a database or applies migrations.

Use these Render service fields:

```text
Name: app-support-hub
Language: Docker
Branch: main
Region: Ohio (US East)
Root Directory: blank
Compute: Free
Dockerfile Path: Dockerfile
Docker Command: blank
Health Check Path: /health/ready
Auto-Deploy: After CI checks pass, if available
```

Configure these Render environment keys. Store the connection string as a
secret and never place its value in source, CI, documentation, logs, command
output, or responses:

```text
PORT=8080
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ConnectionStrings__AppSupportHub=<raw pooled .NET Neon value in Render only>
AppSupportHub__SeedDemoData=false
AppSupportHub__Security__EnableInteractiveLogin=false
```

The forwarded-headers switch lets ASP.NET Core respect Render's terminating
proxy. Keep secrets only in provider settings. Do not commit or print them.

## Database and seed responsibility

The repository owner performs this sequence from a trusted local checkout after
Checkpoint 1 is committed, pushed, and green in CI. Enter the private direct
`.NET` Neon value without echo; never place it in a file, command argument,
documentation, screenshot, or transcript:

```bash
read -r -s -p "Direct Neon connection string: " ASH08B_NEON_DIRECT
printf '\n'
export ConnectionStrings__AppSupportHub="$ASH08B_NEON_DIRECT"
dotnet ef database update \
  --project src/AppSupportHub.Infrastructure/AppSupportHub.Infrastructure.csproj \
  --startup-project src/AppSupportHub.Infrastructure/AppSupportHub.Infrastructure.csproj
export ASPNETCORE_ENVIRONMENT=Production
export AppSupportHub__SeedDemoData=true
dotnet run --project src/AppSupportHub.Web/AppSupportHub.Web.csproj \
  --no-launch-profile -- --seed-fictional-demo-data
unset ConnectionStrings__AppSupportHub ASPNETCORE_ENVIRONMENT AppSupportHub__SeedDemoData
unset ASH08B_NEON_DIRECT
```

Migrate first, then run the seed command once and require successful exit. The
command reuses the existing idempotent three-system/five-work-item fictional
seeder. Without the exact token, normal Production startup never seeds—even if
the gate is accidentally true—and Web never migrates. Do not run EF tools in
the runtime container or change the host environment to Development.

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

## Manual checkpoint

The repository owner reviews and pushes Checkpoint 1, confirms the `CI` workflow,
performs the private direct migration/seed sequence, configures Render with the
private pooled runtime value, and deploys. Resume this task with only the public
Render URL, deployed revision, and sanitized success/failure confirmations.
Provider dashboards and secrets remain outside the local checkpoint.
