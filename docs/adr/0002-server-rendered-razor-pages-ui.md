# ADR 0002: Server-rendered Razor Pages UI

## Status

Accepted

## Context

The planned portal is form- and workflow-oriented, is implemented by a .NET
developer, and must remain achievable within a time-boxed portfolio project. It
does not currently require an offline client, a highly interactive canvas, or
separate frontend deployment.

## Decision

Use ASP.NET Core Razor Pages for the web interface. C# executes on the server;
the browser receives generated HTML, CSS, and limited JavaScript where a real
interaction requires it. Page models remain presentation adapters and delegate
business behavior to Application use cases.

## Consequences

The project uses one language and hosting model for most application work,
supports progressive enhancement, and avoids a separate frontend toolchain.
Server rendering still requires deliberate accessibility, responsive styling,
HTTP security, and efficient request handling. Complex client interaction may
need focused JavaScript later, but it will be added only for demonstrated needs.

## Alternatives considered

- React and Angular were rejected because a separate API-first frontend,
  package ecosystem, build pipeline, and deployment boundary are unnecessary
  for this support portal.
- Blazor was rejected because its additional component/runtime model offers no
  clear advantage for the current form-centric, server-rendered scope.
- MVC was viable but Razor Pages maps more directly to the planned page-focused
  workflows with less ceremony.
