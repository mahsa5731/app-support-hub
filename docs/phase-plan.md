# Approved delivery plan

The phases are deliberately sequential. A later phase depends on the verified
outputs of every earlier phase and must update requirements, decisions, tests,
and the AI-assistance record when its scope changes.

## 1. Architecture and foundation — Completed

- **Objective:** Establish a buildable, testable .NET 10 solution with enforced
  boundaries and an honest minimal host.
- **Main deliverables:** Repository standards, four production projects, three
  test projects, Razor Pages landing page, liveness endpoint, architecture
  tests, requirements, ADRs, and phase documentation.
- **Required tests:** Restore, warning-free build, architecture suite, full test
  suite, format verification, and local `/` and `/health` smoke tests.
- **Dependency:** None.

## 2. Domain and Application core — Completed

- **Objective:** Model the core support concepts and use cases without external
  technology dependencies.
- **Main deliverables:** Domain entities and value objects, workflow rules,
  Application contracts, validation, and feature-oriented organization.
- **Required tests:** Domain and Application unit tests plus continued
  architecture enforcement.
- **Dependency:** Phase 1.

## 3. PostgreSQL and Infrastructure — Completed

- **Objective:** Add relational persistence behind Application contracts.
- **Main deliverables:** PostgreSQL integration, EF Core mappings, migrations,
  constraints, transactions, seed strategy, and development configuration.
- **Required tests:** Mapping, migration, repository integration, constraint,
  and transaction tests against an isolated database.
- **Dependency:** Phases 1–2.

## 4. Core Web and API workflow — Completed

- **Objective:** Deliver the primary application-system and work-item journeys.
- **Main deliverables:** Phase 04A provides bounded read-query ports, read
  models, PostgreSQL projections, and the remaining Systems and WorkItems
  Application mutations. Phase 04B adds accessible Razor Pages workflows, a
  versioned Minimal API and OpenAPI document, shared HTTP validation/error
  mapping, and opt-in fictional Development data.
- **Required tests:** Page, API, validation, workflow, accessibility, and
  persistence integration tests.
- **Dependency:** Phases 1–3.

## 5. Lean change assessment and legacy CSV preview — Completed

- **Objective:** Demonstrate governed change analysis and a safe, read-only
  legacy data boundary.
- **Main deliverables:** Structured assessments, acceptance and rollback plans,
  strict CSV preview, validation, and duplicate detection. Actual import,
  disposition workflows, upload storage, and an import audit trail are neither
  implemented nor planned for this portfolio version.
- **Required tests:** Focused assessment-rule, persistence, malformed-file,
  size-limit, duplicate, preview, and manual journey tests.
- **Dependency:** Phases 1–4.

## 6. Lean security with public live-demo access — Completed

- **Objective:** Keep public read journeys visible while protecting mutations.
- **Main deliverables:** Optional configured accounts, cookie authentication,
  Analyst/Administrator policies, authenticated actors, API/page antiforgery,
  bounded rate limits, secure headers, and partial audit evidence.
- **Required tests:** Authentication, role-matrix, access-denial, rate-limit,
  input-security, secret-safety, and audit tests.
- **Dependency:** Phases 1–5.

## 7. Lean operational readiness — Complete

- **Objective:** Add explainable operational evidence for the small public demo.
- **Main deliverables:** Separate PostgreSQL readiness, correlation-scoped
  completion logging, one bounded public Operations projection/page, qualitative
  query review, troubleshooting runbook, and manual journey.
- **Required tests:** Overview projection, public rendering, health failure mode,
  and correlation behavior.
- **Dependency:** Phases 1–6.

General reporting/export, configurable dashboards, and list pagination are
intentionally removed from this portfolio scope; existing bounds suit the
fictional dataset.

## 8. CI/CD, Docker, deployment, and final documentation — In progress

- **Phase 08A — Completed:** A cache-aware multi-stage, non-root .NET 10
  container; read-only GitHub Actions validation; local container smoke evidence;
  and the Render/Neon environment, migration, health, and secret handoff.
- **Phase 08B — Prepared, external release pending:** The narrow explicit,
  idempotent fictional Production seed command is ready for local validation.
  Owner-controlled Neon migration/seed, Render deployment, public read-only URL,
  release/rollback evidence, and final documentation remain pending.
- **Required tests:** Clean CI build, container health, migration/deployment,
  rollback, environment, and deployed smoke tests.
- **Dependency:** Phases 1–7.

## Portfolio scope guard

Phases 06–08 each deliver only one demonstrable vertical slice, reuse the
current code, and normally add no more than eight tests. They do not aim for
production completeness unless the user explicitly changes this scope.

## 9. Ultra-mode independent final audit — Planned

- **Objective:** Perform an independent, adversarial review of the complete
  repository against requirements, architecture, security, quality, and honest
  portfolio claims.
- **Main deliverables:** Evidence-backed findings, prioritized remediation,
  final traceability, and an explicit go/no-go assessment.
- **Required tests:** Full clean-room validation plus targeted tests for every
  material audit finding.
- **Dependency:** Phases 1–8 complete.
