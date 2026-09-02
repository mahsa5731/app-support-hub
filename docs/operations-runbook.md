# Operations runbook

This is a lean support guide for the fictional AppSupportHub portfolio demo,
not a production SLA or monitoring plan. The provider contract is in the
[deployment handoff](deployment.md).

## Startup requirements

- Install the repository-selected .NET 10 SDK and make PostgreSQL reachable.
- Set `ConnectionStrings__AppSupportHub`; never commit or print its value.
- Apply both migrations explicitly before the Web host starts:
  `dotnet ef database update --project src/AppSupportHub.Infrastructure`.
- Keep `AppSupportHub__SeedDemoData=false` during normal Production startup.
- Production fictional data may be inserted only after explicit migration by
  invoking `--seed-fictional-demo-data` once with Production and the seed gate;
  the command is idempotent and exits without starting Web.
- Interactive login is optional; when disabled, the demo remains public read-only.

## Health and diagnostics

- `GET /health` is process liveness and never contacts PostgreSQL.
- `GET /health/ready` checks PostgreSQL and returns only `Healthy` or `Unhealthy`.
- `X-Correlation-ID` accepts a GUID and returns lowercase `N` form; invalid or
  absent values are replaced. Use it to connect a response to scoped logs.
- Safe completion fields are correlation ID, method, path without query string,
  status, and elapsed milliseconds. Never collect bodies, cookies, authorization,
  antiforgery values, credentials, CSV rows, assessments, or connection data.

## Common failures

- Startup configuration error: confirm the connection-string key exists without
  exposing its value; optional login errors identify configuration keys only.
- Liveness 200/readiness 503: check PostgreSQL process/network access, database
  name/user permissions, and whether migrations ran. The host can remain alive.
- Operations error: verify readiness first, then use the correlation ID and safe
  completion event. Do not return infrastructure exceptions to the requester.
- Migration failure: stop rollout, preserve logs without secrets, and escalate;
  never make Web automatically migrate or edit migration history manually.
- Seed failure: stop before deployment and investigate with sanitized evidence;
  never rerun blindly, delete records manually, or expose connection details.

## Shutdown and escalation

Stop Web gracefully, then stop only the disposable/local PostgreSQL instance.
Escalate repeated readiness or migration failures with UTC time, correlation ID,
health status, deployed revision, and sanitized error category. Keep secrets,
hostnames, SQL text, customer data, and generated dumps out of tickets.

## Phase 08B handoff checklist

- Inspect GitHub repository state and the revision selected for deployment.
- Create the Render Web Service from the root `Dockerfile` and Neon database.
- Store the connection string and other secrets only in provider settings.
- Apply both migrations from a trusted local environment before Web starts.
- Keep login and the Development-only seed disabled; use only the separately
  approved narrow fictional Production seed.
- Inspect optional Analyst/Administrator login configuration.
- Inspect liveness/readiness public health URLs.
- Inspect provider-specific rollback steps before release.

For rollback, redeploy the last known-good application revision. Database
migrations and fictional seed records are not automatically reversed; preserve
them unless a separately reviewed data recovery action is required.
