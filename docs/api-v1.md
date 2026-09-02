# REST API v1

AppSupportHub exposes a path-versioned JSON API at `/api/v1`. The generated
OpenAPI document is available at `/openapi/v1.json`; no Swagger UI is included.
Dates use ISO 8601 with an explicit offset. Enum-like values are documented
strings and are matched case-insensitively.

## Routes

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/v1/systems` | Bounded system list with name, type, criticality, lifecycle, and limit filters |
| GET | `/api/v1/systems/{id}` | System detail |
| POST | `/api/v1/systems` | Create a system |
| PUT | `/api/v1/systems/{id}` | Update system metadata |
| POST | `/api/v1/systems/{id}/lifecycle` | Apply a lifecycle transition |
| GET | `/api/v1/work-items` | Bounded work-item list with system, title, type, priority, status, assignee, overdue, and limit filters |
| GET | `/api/v1/work-items/{id}` | Work-item detail and immutable history |
| POST | `/api/v1/work-items` | Create a work item |
| PUT | `/api/v1/work-items/{id}` | Update title and description |
| PUT | `/api/v1/work-items/{id}/assignment` | Assign a work item |
| DELETE | `/api/v1/work-items/{id}/assignment` | Remove assignment |
| PUT | `/api/v1/work-items/{id}/priority` | Change priority |
| PUT | `/api/v1/work-items/{id}/due-date` | Set or clear a due date |
| POST | `/api/v1/work-items/{id}/transitions` | Apply a status transition or resolution |

List limits must be from 1 through 100 and default to 50. Pagination is planned
for a later phase. Route identifiers are authoritative; mutation bodies contain
neither an ID nor an actor identifier.

## Examples

Create a fictional custom system:

```http
POST /api/v1/systems
Content-Type: application/json

{
  "name": "Example Support Sandbox",
  "description": "Fictional local demonstration system.",
  "type": "Custom",
  "criticality": "High",
  "initialLifecycleStatus": "Active",
  "businessOwner": "Example Business Practice",
  "technicalOwner": "Example Technical Practice",
  "supportTeam": "Example Support Lab",
  "vendorName": null
}
```

A successful create returns `201 Created`, a body containing the generated ID,
and `Location: /api/v1/systems/{id}`.

Create a work item with an explicit UTC offset:

```http
POST /api/v1/work-items
Content-Type: application/json

{
  "applicationSystemId": "00000000-0000-0000-0000-000000000001",
  "type": "Incident",
  "title": "Investigate fictional delay",
  "description": "Synthetic demonstration only.",
  "priority": "High",
  "dueAt": "2035-02-03T12:30:00Z"
}
```

Set a status and resolution through the transition route; Domain/Application
still determine whether the transition is valid:

```http
POST /api/v1/work-items/00000000-0000-0000-0000-000000000002/transitions
Content-Type: application/json

{
  "targetStatus": "Resolved",
  "comment": "Fictional validation completed.",
  "resolutionSummary": "Synthetic issue resolved."
}
```

## Status and error mapping

Gets and lists return `200`. Updates and actions return `200` with
`{"changed":true|false}`. Creates return `201`. Expected failures use RFC 7807
`application/problem+json` with `status`, a safe `detail`, and the stable
Application error code in `code`:

| Application error type | HTTP status |
| --- | --- |
| Validation | 400 Bad Request |
| NotFound | 404 Not Found |
| Conflict | 409 Conflict |
| BusinessRule | 409 Conflict |

Responses do not include SQL, connection data, stack traces, aggregates, or EF
entities.

## Demo identity and scope

Phase 04 has no authentication or authorization. Every material mutation uses
the server-owned synthetic actor `demo.user@appsupporthub.local`; this does not
represent an authenticated user. Client actor fields are not part of the API
contract and cannot select the persisted history actor. Do not expose this demo
API as a production service.
