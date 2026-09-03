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
- Live checks are
  [`/health`](https://app-support-hub.onrender.com/health) and
  [`/health/ready`](https://app-support-hub.onrender.com/health/ready); allow one
  bounded retry when the Render Free service is cold.
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

## Live release and rollback

The public demo is
[https://app-support-hub.onrender.com/](https://app-support-hub.onrender.com/).
Before release, confirm the selected `main` revision and green GitHub Actions CI.
After Render deploys it, verify readiness, the bounded public routes, and the
fictional 3-system/5-work-item counts without authenticating or mutating data.

For an application regression, redeploy the last known-good Render revision and
recheck `/health/ready`. Database migrations and fictional seed records are not
automatically reversed; preserve them and escalate compatibility concerns rather
than editing migration history or deleting records. Keep provider secrets and
internal database details out of tickets and documentation.
