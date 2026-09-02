# Phase 04 manual test script

Use only fictional records in a local Development environment. Complete the
database migration and startup steps in [local development](local-development.md).
This script is a focused acceptance aid, not security or WCAG certification.

## Keyboard and shared layout

1. Open `/` over HTTPS without using a mouse.
2. Tab to “Skip to main content”, activate it, and verify focus moves to main.
3. Tab through Home, Systems, Work Items, and API document; verify visible focus
   and an obvious current-page style.
4. Resize to a narrow viewport and confirm content and navigation remain usable.
5. Verify the Phase 06, public-demo, and independent/non-affiliation statements.

## Systems journey

1. Open `/Systems`, verify the useful empty state or fictional seed list, and
   exercise name, type, criticality, and lifecycle filters.
2. Open Create. Submit empty fields and verify the summary/field feedback is
   understandable and programmatically adjacent to labelled controls.
3. Create a fictional Custom Active system and verify the canonical detail
   page appears after the redirect.
4. Edit metadata, save, and verify the updated detail after redirect.
5. Attempt a lifecycle action without confirmation and verify no mutation.
6. Confirm a valid lifecycle change. For retirement, supply a reason and verify
   the UTC retired time and reason. Confirm there is no delete operation.

## Work-item journey

1. Open `/WorkItems`, exercise every filter, and confirm status/priority text is
   readable. Verify an overdue item says “Overdue” rather than relying on color.
2. Create a fictional Incident for a non-retired system. Enter the optional
   `datetime-local` due date as explicitly labelled UTC.
3. Edit title/description and return to canonical detail.
4. Assign, change priority, change/clear due date, and apply valid status
   transitions. Resolve only through a valid type-specific path and supply a
   resolution summary.
5. Verify every success uses a redirect, detail shows system/resolution data,
   and immutable history remains chronological with the authenticated actor.
6. Confirm no delete or history-edit control exists.

## REST API

1. Open `/openapi/v1.json` and confirm the `v1` document describes all 15 routes
   listed in [API v1](api-v1.md).
2. Use `curl` or another local client to create/get/list/update one fictional
   system and work item. Confirm create returns 201 and a detail Location.
3. Send an invalid choice and verify 400 ProblemDetails with
   `code: validation.invalid_input`.
4. Request a random ID and verify 404 with a stable code; attempt an invalid
   transition and verify 409.
5. Add an `actorIdentifier` property to a mutation body. Verify history still
   records only the authenticated username.
6. Verify `/health` returns 200 and contains no database-readiness claim.

## Phase 05 change assessment

1. Open a fictional ChangeRequest detail, follow “Change assessment”, complete
   all narratives and risk, save, and verify PRG reloads the same canonical URL
   with trimmed values, UTC metadata, and the authenticated actor.
2. Save identical values again, then edit one value; verify the form remains
   usable and no assessment link appears on Incident or Enhancement details.

## Phase 05 legacy CSV preview

1. Open `/LegacyImports`, download the fictional sample, upload it, and verify
   Ready, Review duplicate, and Reject counts plus text/icon row dispositions.
2. Verify the page states that it never imports or stores records. Try a wrong
   header and an oversized/non-CSV file and confirm safe feedback; then verify
   the Systems count did not change and no import/confirmation control exists.

## Phase 06 public and role matrix

1. With interactive login disabled, verify public lists/details, assessment
   display, CSV instructions/sample, GET API, OpenAPI, and health work while all
   mutation controls/routes are unavailable or challenge.
2. Enable externally configured fictional accounts. Verify generic login
   failure, secure cookie navigation identity, POST logout, and the sixth failed
   login attempt returning 429.
3. As Analyst, change a WorkItem/save an assessment/preview CSV and confirm the
   authenticated username persists; verify System administration is denied.
4. As Administrator, perform a System mutation. For unsafe API calls, verify a
   cookie alone fails and the supporting antiforgery header succeeds.

Record browser, date, pass/fail, and any observation outside the repository.
Do not enter a real person, organization, credential, customer, or production
record.
