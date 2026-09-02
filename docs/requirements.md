# AppSupportHub requirements

## Purpose and status

These requirements define the intended portfolio product. Phase 02 implements
the Systems and WorkItems Domain/Application core. Persistence, user-facing
workflows, security, integrations, and operations remain later-phase work.

## Functional requirements

### Application systems

- **FR-SYS-001 — Application catalog:** Authorized users shall create, view,
  update, search, and retire application-system records.
- **FR-SYS-002 — Classification:** Each application system shall be classified
  as commercial or custom and shall retain relevant ownership, support, and
  lifecycle metadata.

### Support work

- **FR-WRK-001 — Work-item types:** The system shall represent incidents,
  enhancements, and change requests as explicitly identified work-item types.
- **FR-WRK-002 — Assignment and priority:** Authorized users shall assign work
  items and record their priority.
- **FR-WRK-003 — Workflow:** Work items shall follow validated status
  transitions appropriate to their type.
- **FR-WRK-004 — Scheduling and resolution:** Work items shall support due dates
  and structured resolution details.
- **FR-WRK-005 — Immutable history:** Material changes to a work item shall
  append immutable history entries identifying what changed, when, and by whom.

### Change assessment

- **FR-CHG-001 — Assessment:** A change request shall support a structured
  assessment containing business need, technical impact, security impact,
  risk, acceptance criteria, test plan, and rollback plan.

### Legacy integration boundary

- **FR-IMP-001 — CSV preview:** Authorized users shall preview a supported
  legacy CSV export before importing any records.
- **FR-IMP-002 — Validation:** The import boundary shall validate file shape,
  field values, size limits, and record-level errors without partially applying
  an invalid batch.
- **FR-IMP-003 — Duplicate detection:** The preview shall identify probable
  duplicates and require an explicit supported disposition before import.

### Information access and operations

- **FR-REP-001 — Dashboard:** Authorized users shall view operational summaries
  for application systems and work items.
- **FR-REP-002 — Reporting:** Authorized users shall filter and export defined
  support and change-management reports.
- **FR-SEC-001 — Role-based access:** The system shall authorize operations by
  defined roles and enforce access on the server.
- **FR-API-001 — REST API:** The system shall expose a documented, versioned REST
  API for supported business operations.
- **FR-OPS-001 — Operational health:** The host shall expose health information
  suitable for liveness and later readiness monitoring. Phase 01 provides only
  the liveness baseline.
- **FR-AUD-001 — Auditability:** Security-sensitive and material business
  operations shall produce traceable audit records protected from ordinary
  application updates.

## Phase 02 implementation traceability

| Requirement | Phase 02 Domain/Application evidence | Remaining delivery |
| --- | --- | --- |
| FR-SYS-001 | ApplicationSystem creation, metadata, lifecycle, and retirement rules; create and retire use cases | Persistence, retrieval/search, authorization, and user/API workflows |
| FR-SYS-002 | Commercial/custom classification, ownership, support team, criticality, lifecycle, and commercial-vendor invariant | Persistence and user-facing maintenance |
| FR-WRK-001 | Incident, enhancement, and change-request types plus validated creation use case | Persistence and user/API creation workflow |
| FR-WRK-002 | Assignment, priority, and due-date Domain behavior; assignment use case | Persistence, priority/due-date use cases, authorization, and UI/API |
| FR-WRK-003 | Exact type-aware status matrix, `CanTransitionTo`, and transition use case | Persistence, authorization, and user/API workflow |
| FR-WRK-004 | Due-date validation, overdue calculation, resolution, reopen, and close behavior | Persistence and user-facing scheduling/resolution workflow |
| FR-WRK-005 | WorkItem-controlled immutable history with structured event types and values | Durable storage, actor identity, and history presentation |
| FR-OPS-001 | Phase 01 liveness endpoint remains implemented | Readiness dependencies and operational monitoring in Phase 7 |

No functional requirement is marked fully delivered because the Phase 02 core
has no persistence or user-accessible business workflow. Change assessment,
legacy import, reporting, role-based access, REST API, and security audit
requirements remain planned for their approved later phases.

## Non-functional requirements

- **NFR-MNT-001 — Maintainability:** The codebase shall preserve explicit layer
  boundaries, feature-oriented organization, cohesive naming, and minimal
  dependencies.
- **NFR-SEC-001 — Security:** Inputs shall be validated; authentication,
  authorization, secure defaults, rate limiting, and secret-safe configuration
  shall be applied before business deployment.
- **NFR-PRV-001 — Privacy:** The system shall collect only necessary portfolio
  demonstration data and shall not contain actual City, employer, customer, or
  production data.
- **NFR-ACC-001 — Accessibility:** User interfaces shall target WCAG 2.2 AA,
  including semantic structure, keyboard access, visible focus, sufficient
  contrast, and understandable validation feedback.
- **NFR-PRF-001 — Performance:** List and reporting endpoints shall use bounded
  queries and server-side pagination; measurable response-time targets shall be
  defined with representative data before release.
- **NFR-TST-001 — Testability:** Business rules shall be isolated from framework
  code and covered by unit, integration, architecture, and documented manual
  tests appropriate to their risk.
- **NFR-OBS-001 — Observability:** The service shall provide structured logging,
  health checks, correlation context, and actionable troubleshooting guidance
  without logging secrets or sensitive values.
- **NFR-DAT-001 — Data integrity:** Relational constraints, transactions,
  concurrency decisions, and immutable history shall protect consistency.
- **NFR-BLD-001 — Reproducible builds:** SDK selection, dependency versions,
  analyzers, formatting, and deterministic compilation shall be repository
  controlled.
- **NFR-DEP-001 — Deployment portability:** Runtime configuration shall be
  environment based and the later application shall be deployable without
  coupling business code to a single hosting vendor.
- **NFR-DOC-001 — Documentation:** Requirements, architecture decisions,
  operations, testing, AI assistance, and known limitations shall remain
  accurate as the project evolves.

## Out of scope

The project excludes actual City data, actual employer systems, PowerBuilder
code, Classic ASP code, Oracle or PL/SQL implementation, microservices, native
mobile applications, email delivery, and permanent file storage. The future CSV
import boundary demonstrates integration design only; it does not claim to
implement or operate any employer technology.
