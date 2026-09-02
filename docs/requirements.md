# AppSupportHub requirements

## Purpose and status

These requirements define the intended portfolio product. Phase 06 keeps the
catalog publicly readable and protects mutations with optional configured
portfolio accounts, roles, authenticated actors, and request controls.

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

- **FR-IMP-001 — CSV preview:** Users shall preview a supported legacy CSV
  export without importing or storing records.
- **FR-IMP-002 — Validation:** The import boundary shall validate file shape,
  field values, size limits, and record-level errors without applying writes.
- **FR-IMP-003 — Duplicate detection:** The preview shall identify probable
  duplicates for review. Disposition selection and import are out of scope.

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

## Implementation traceability through Phase 06

| Requirement | Implemented evidence through Phase 06 | Remaining delivery |
| --- | --- | --- |
| FR-SYS-001 | Razor and API create, get, bounded search/filter, update, and confirmed lifecycle/retirement workflows persist through Application handlers | Authorization |
| FR-SYS-002 | Commercial/custom pages and DTOs present ownership, support, vendor, criticality, lifecycle, retirement, and labelled UTC metadata | Authorization |
| FR-WRK-001 | Razor and API creation represent incidents, enhancements, and change requests against a required non-retired system | Authorization |
| FR-WRK-002 | Razor named actions and API endpoints provide assignment, unassignment, priority, and bounded filters | Authorization and authenticated identity |
| FR-WRK-003 | Razor/API status actions delegate the exact type-aware transition matrix to Domain/Application | Authorization |
| FR-WRK-004 | Razor/API due-date and detail workflows present overdue text and resolution data with explicit UTC handling | Authorization |
| FR-WRK-005 | Detail pages/API project chronological immutable history; user mutations persist the authenticated configured username | Persistent enterprise identity and centralized audit |
| FR-CHG-001 | One persisted structured assessment per ChangeRequest, with bounded narratives, risk, plans, timestamps, idempotent save, and Razor PRG workflow | Authorization and authenticated assessor identity |
| FR-IMP-001 | Strict UTF-8 `.csv` upload previews up to 256 KiB and 100 data rows; no upload or record is stored | Partially demonstrated: authorization is absent and actual import is intentionally not planned |
| FR-IMP-002 | Exact header, shape, vocabulary, field, vendor, size, encoding, and row-limit validation produces safe row/file feedback without database writes | Partially demonstrated by one bounded fictional format |
| FR-IMP-003 | Case-insensitive in-file LegacyId/name and PostgreSQL name duplicates are labelled for review | Partially demonstrated: no disposition or import workflow is planned |
| FR-SEC-001 | Public reads plus Administrator-only System writes and Analyst/Administrator WorkItem, assessment, and CSV writes are enforced server-side | Portfolio roles only; no enterprise identity lifecycle |
| FR-API-001 | Fourteen business routes plus one supporting antiforgery-token route, OpenAPI, cookie authorization, 401/403/429, and CSRF validation are HTTP tested | Later feature APIs |
| FR-AUD-001 | Structured login/logout events and authenticated actors in existing WorkItem history/assessment fields provide bounded evidence | Partial: System audit and tamper-resistant centralized logs are absent |
| FR-OPS-001 | Phase 01 liveness endpoint remains implemented | Readiness dependencies and operational monitoring in Phase 7 |

Phase 06 satisfies the portfolio role matrix only when externally configured.
With login disabled it intentionally remains public read-only. Actual import,
persistent identity, full business audit, and production security remain absent.

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

Phase 03 evidence for NFR-DAT-001 includes named PostgreSQL constraints,
foreign keys, atomic EF Core saves, `xmin` concurrency checks, unique
case-insensitive system names, and append-only ordered history tested on real
PostgreSQL. The repository-local migration and pinned tool/package versions
also advance NFR-BLD-001 without introducing runtime secrets.

Phase 04A advances NFR-MNT-001, NFR-PRF-001, and NFR-TST-001 through explicit
Application-owned query ports and presentation-neutral read models, bounded
server-side no-tracking projections, deterministic ordering, cancellation, and
focused unit, PostgreSQL integration, and architecture coverage. A limit is not
cursor or page-number pagination; full pagination remains later work.

Phase 04B adds thin Web adapters, one case-insensitive Application parsing
boundary, server validation, antiforgery and PRG, visible keyboard focus,
semantic landmarks, non-color status text, RFC 7807 errors, HTTPS HTTP tests,
and an opt-in fictional seed gate. These checks provide accessibility evidence,
not WCAG certification; security and operational non-functional requirements
remain incomplete.

Phase 05 advances NFR-MNT-001, NFR-DAT-001, and NFR-TST-001 with a Domain-owned
assessment invariant, one-to-one PostgreSQL constraints, a bounded Application
preview port, and focused real-PostgreSQL tests. One indexed database name check
per unique valid row is an explicit simple tradeoff under the 100-row ceiling.

## Out of scope

The project excludes actual City data, actual employer systems, PowerBuilder
code, Classic ASP code, Oracle or PL/SQL implementation, microservices, native
mobile applications, email delivery, permanent file storage, and actual legacy
data import. The CSV preview boundary demonstrates integration design only; it
does not claim to implement or operate any employer technology.
