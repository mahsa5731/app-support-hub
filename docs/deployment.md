# Deployment

The public read-only demo is
[https://app-support-hub.onrender.com/](https://app-support-hub.onrender.com/).
The verified Phase 09 baseline is commit
`1f058a656539a4484667d6012794652602eb94e1`.

## Deployed shape

- Render Web Service `app-support-hub`, Docker, Ohio (US East), Free tier
- Repository `mahsa5731/app-support-hub`, branch `main`, root Dockerfile
- Neon PostgreSQL project `app-support-hub`, production branch, AWS `us-east-2`
- Non-root .NET 10 runtime container listening on HTTP port `8080`
- Provider-terminated HTTPS with ASP.NET Core forwarded headers
- Public anonymous reads, fictional data, and interactive login disabled

Render's free tier may cold-start after inactivity. Liveness is `/health`;
`/health/ready` is the PostgreSQL-aware deployment health check.

The service configuration is:

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

Keep runtime configuration in Render settings:

```text
PORT=8080
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ConnectionStrings__AppSupportHub=<raw pooled .NET Neon value in Render only>
AppSupportHub__SeedDemoData=false
AppSupportHub__Security__EnableInteractiveLogin=false
```

Never commit, print, document, or return a connection value or credential.

## Database migration and fictional seed

Use the direct, non-pooled `.NET` Neon value only from a trusted local shell for
EF migration and the explicit seed command. Use the pooled `.NET` value only
for the Render runtime. Web startup never creates, migrates, or seeds a database.

For zsh, read the direct value without echo or shell-history exposure:

```zsh
IFS= read -r -s 'ASH_NEON_DIRECT?Direct Neon .NET connection string: '
printf '\n'
export ConnectionStrings__AppSupportHub="$ASH_NEON_DIRECT"
dotnet ef database update \
  --project src/AppSupportHub.Infrastructure/AppSupportHub.Infrastructure.csproj \
  --startup-project src/AppSupportHub.Infrastructure/AppSupportHub.Infrastructure.csproj
export ASPNETCORE_ENVIRONMENT=Production
export AppSupportHub__SeedDemoData=true
dotnet run --project src/AppSupportHub.Web/AppSupportHub.Web.csproj \
  --no-launch-profile -- --seed-fictional-demo-data
unset ConnectionStrings__AppSupportHub ASPNETCORE_ENVIRONMENT AppSupportHub__SeedDemoData
unset ASH_NEON_DIRECT
```

Apply the two migrations first. Run the idempotent fictional seed only for an
empty demo database or an explicitly reviewed recovery; require successful exit
with three systems and five work items. Never switch the host to Development,
pass the connection as a command argument, or run EF tools in the runtime image.

## Release verification

After a green CI run and Render deployment, allow one bounded cold-start retry,
then require HTTPS and HTTP 200 from:

- `/`, `/Systems`, `/WorkItems`, and `/Operations`
- `/api/v1/systems` and `/api/v1/work-items`
- `/openapi/v1.json`
- `/health` and `/health/ready`

Confirm the APIs expose only the three fictional systems and five fictional work
items, anonymous reads work, security/correlation headers remain present, and
responses contain no credential, connection, provider-internal, or exception
detail.

## Redeploy and rollback

Confirm the selected `main` revision and green CI result before deployment.
After auto-deploy, record the revision and sanitized health results. For an
application regression, redeploy the last known-good Render revision and recheck
`/health/ready` plus the bounded public routes.

Application rollback does not reverse database migrations or fictional seed
records. Stop and review database compatibility rather than editing migration
history or deleting records manually. See the
[operations runbook](operations-runbook.md) for incident handling.
