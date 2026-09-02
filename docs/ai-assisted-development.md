# AI-assisted development record

## Principle

AI accelerates implementation, but scope, acceptance, verification, and
presentation remain human responsibilities. Generated work must be inspectable,
tested, and described without overstating authorship or review.

## Phase 01

Codex generated the repository foundation: the classic solution, seven .NET 10
projects, reference graph, central build and package configuration, editor and
Git standards, Razor Pages foundation host, liveness endpoint, assembly markers,
architecture tests, and initial project documentation.

The user-supplied specification decided the product name and location, four-layer
architecture, exact reference graph, .NET and C# versions, Razor Pages and xUnit
choices, central package policy, health endpoint, documentation set, phase
boundaries, non-affiliation language, and explicit exclusions. Codex selected
only conventional implementation details needed to express those decisions.

Validation completed:

```text
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
git --version
dotnet sln AppSupportHub.sln list
dotnet restore AppSupportHub.sln
dotnet build AppSupportHub.sln --no-restore
dotnet test tests/AppSupportHub.ArchitectureTests/AppSupportHub.ArchitectureTests.csproj --no-build
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
dotnet run --project src/AppSupportHub.Web/AppSupportHub.Web.csproj --no-build --launch-profile https
```

Restore passed. The final build passed with zero warnings and zero errors. All
six architecture tests passed, the full solution test command passed with the
same six tests, and formatting verification reported no changes. Local requests
to `/` and `/health` began over HTTP, followed the configured HTTPS redirect,
and returned HTTP 200. The landing page contained the product name, phase label,
and non-affiliation statement; the health response was `Healthy`. The verified
host run shut down cleanly with no exception in its console.

The .NET template engine required its normal per-user template cache during
initial generation. NuGet restore and the test/web processes required approved
network or local-socket access in the execution sandbox. An initial HTTP/2 curl
run exposed a local macOS/.NET TLS flush error after successful responses; the
host was restarted and the required smoke checks were repeated over HTTP/1.1
without an exception. This did not require a code or configuration workaround.
All repository deliverables were created in the specified target. There are no
specification deviations or unresolved Phase 01 issues.

This document must be updated in every later phase with the assistance actually
used, decisions retained by the human owner, validation performed, deviations,
and unresolved issues. No claim is made that generated code has received a
line-by-line human review.

## Phase 02

Codex implemented the Systems and WorkItems Domain modules, encapsulated
lifecycle and workflow invariants, immutable work-item history, specific
Application persistence ports, the structured result model, five explicit use
case handlers, handwritten UnitTests doubles, focused architecture assertions,
and Phase 02 documentation/status updates.

The specification fixed the public state, enums, length limits, exact transition
matrices, history conventions, error codes, use cases, repository interfaces,
TimeProvider boundary, test strategy, and later-phase exclusions. Codex made
only local implementation choices needed to express those decisions.

Validation performed before the final full-suite gate:

```text
dotnet restore AppSupportHub.sln
dotnet build AppSupportHub.sln --no-restore
dotnet test AppSupportHub.sln --no-build
dotnet format AppSupportHub.sln --verify-no-changes --no-restore
dotnet test tests/AppSupportHub.UnitTests/AppSupportHub.UnitTests.csproj --filter FullyQualifiedName~Domain.Systems
dotnet test tests/AppSupportHub.UnitTests/AppSupportHub.UnitTests.csproj --filter FullyQualifiedName~Domain.WorkItems
dotnet test tests/AppSupportHub.UnitTests/AppSupportHub.UnitTests.csproj --filter FullyQualifiedName~UnitTests.Application
dotnet test tests/AppSupportHub.ArchitectureTests/AppSupportHub.ArchitectureTests.csproj
```

The targeted Systems run passed 39 cases before two final invariant tests were
added. The combined final targeted Domain run passed 146 cases, the targeted
Application run passed 28 cases, and the expanded architecture run passed 8
tests. The final integrated build passed with zero warnings and zero errors.
The unit test suite passed 174 tests, the architecture test suite passed 8
tests, and the full solution passed 182 tests; the IntegrationTests project
remains intentionally empty. Formatting passed after formatter-required
naming-only test renames, and the final diff check passed. No new package,
production dependency, persistence implementation, or Web business workflow
was introduced. A local host smoke test returned `Healthy` from `/health` and
the Phase 02 status page from `/`. There were no deviations or unresolved
issues. No line-by-line human review is claimed.
