# AGENTS.md

Guidance for AI agents working on the client application for the Diploma Control System.

This file is client-focused. Use the server project at `src/DiplomaControlSystem.Server` as the source of truth for API contracts, domain rules, validation behavior, and endpoint semantics.

## Working Principles

- Read the relevant server feature before changing client behavior.
- Treat `src/DiplomaControlSystem.Server/DiplomaControlSystem.Api/Features` as the API contract source.
- Keep client models aligned with server contracts:
  - Incoming payloads to the server are named `FeatureNameRequest`.
  - Server responses are named `FeatureNameResponse`.
  - Shared resource responses may use domain names such as `DiplomaExaminationCommissionResponse`.
- Do not invent client-only request or response shapes when the server already has a contract.
- Prefer small feature-oriented modules on the client: API function, query/mutation hook, form schema, page/component.
- Preserve the domain language used by the server. In particular, use `DefenseYear` for the actual diploma defence year.

## API Base

All API routes are mounted under:

```text
/api
```

Expected client API areas:

- Groups: `/api/groups`
- Students: `/api/students`
- Diploma examination commissions: `/api/diploma-examination-commissions`

Use the server route mappings in `DiplomaControlSystem.Api/Extensions/WebApplicationExtension.cs` and each feature's `Endpoint.MapEndpoint` method as the canonical endpoint list.

## Domain Rules The Client Must Respect

### Defense Year

The group year stored by the server is the actual defence year.

Example:

```text
DefenseYear: 2026
Academic display year: 2025/26
```

Client forms for creating or editing groups and commissions must ask the user for the actual defence year only, for example `2026`.

Do not send academic-year strings such as `2025/26` back to the server as a year value.

### Allowed Group Year Range

For group create/update, the defence year must be:

```text
current Ukraine year <= DefenseYear <= current Ukraine year + 2
```

The server validates this using Ukraine time. The client may mirror the validation for UX, but the server remains authoritative.

### Education Level

Valid education levels are:

```text
Bachelor
Master
```

Keep client values aligned with server enum strings. Avoid localized enum values in API payloads.

### Secretary Access

Most read/write operations are scoped by `SecretaryEmail`. The client should keep this value available for API calls that require it.

If the API returns `403`, treat it as an access/specialty mismatch or inactive secretary state, not as a generic network failure.

### Dates

Use ISO date-only strings for `DateOnly` values:

```text
YYYY-MM-DD
```

Do not send local date-time strings with time zones for date-only fields.

For DEC creation/update, `StartDate` and `EndDate` must belong to the selected `DefenseYear`, and `EndDate` must be greater than or equal to `StartDate`.

## Main API Surface

### Groups

- `GET /api/groups/academic-years`
  - Query: `secretaryEmail`, `educationLevel`
  - Returns academic years with `Year`, `DefenseYear`, and groups.
  - `Year` is display-only academic year, for example `2025/26`.
  - `DefenseYear` is the full actual year, for example `2026`.

- `POST /api/groups`
  - Content type: `multipart/form-data`
  - Body fields: `secretaryEmail`, `name`, `year`, `educationLevel`, and either `studentsFile` or `googleDriveUrl`.
  - Send `year` as the actual defence year.

- `PATCH /api/groups/{groupId}`
  - Update group metadata.
  - Send `year` as the actual defence year.

- `DELETE /api/groups/{groupId}`
  - Deletes a group only when server rules allow it.

- `GET /api/groups/{groupId}/students`
  - Query: `secretaryEmail`
  - Returns students and checklist state for group screens.

- `GET /api/groups/{groupId}/statistics`
  - Query: `secretaryEmail`
  - Returns defence-result statistics for charts/summary panels.

### Students

- `POST /api/groups/{groupId}/students`
  - Adds a student with default diploma data.

- `GET /api/students/{studentId}/details`
  - Query: `secretaryEmail`
  - Returns the full student diploma process state.

- `DELETE /api/students/{studentId}`
  - Deletes a student and related default diploma data.

- `GET /api/students/{studentId}/qualification-work-options`
  - Query: `secretaryEmail`
  - Returns supervisor and reviewer options.

- `PATCH /api/students/{studentId}/name`
- `PATCH /api/students/{studentId}/qualification-work`
- `PATCH /api/students/{studentId}/physical-checklist`
- `PATCH /api/students/{studentId}/electronic-checklist`
- `PATCH /api/students/{studentId}/defence`
- `PATCH /api/students/{studentId}/defence-results`
- `PATCH /api/students/{studentId}/qualification-work-characteristics`

For student PATCH endpoints, use the matching server `Update...Request` and `Update...Response` contracts.

### Diploma Examination Commissions

- `GET /api/diploma-examination-commissions`
  - Query: `secretaryEmail`, `educationLevel`, `defenseYear`
  - Returns commissions for the selected specialty, education level, and defence year.

- `GET /api/diploma-examination-commissions/options`
  - Query: `secretaryEmail`, `educationLevel`, `defenseYear`
  - Returns groups, teachers, and secretary data needed by the DEC form.

- `POST /api/diploma-examination-commissions`
  - Creates a commission.

- `PUT /api/diploma-examination-commissions/{commissionId}`
  - Updates a commission.

- `DELETE /api/diploma-examination-commissions/{commissionId}`
  - Deletes a commission without deleting archived defence data.

DEC form rules:

- A commission must have at least one group.
- Groups are filtered by secretary specialty, education level, and defence year.
- The head can be either a teacher or an invited person.
- If the head is invited, both name and position are required.
- Commission members must be teachers.
- Head, members, and secretary must be different people.
- Dates must be within the selected defence year.

## Client Implementation Rules

### API Layer

- Keep a typed API function per server feature.
- Use the server feature name in client API function names where practical.
- Keep request/response TypeScript types close to the API function or in a feature-local `types.ts`.
- Use `FormData` only for endpoints that require multipart data, such as group creation with student import.
- Keep JSON payload keys camelCase unless the server contract explicitly requires otherwise.

### Validation

- Mirror server validation in the UI for immediate feedback, but do not rely on client validation as security.
- When using a schema library, name schemas after server contracts, for example `createGroupRequestSchema`.
- Show server validation errors returned as problem details near the relevant fields.

### State Management

- Server state should be loaded through query/mutation primitives.
- Invalidate or refresh related queries after mutations:
  - Group create/update/delete should refresh academic years and group lists.
  - Student updates should refresh group students and student details.
  - DEC create/update/delete should refresh DEC list and DEC options when group assignment may change.
- Avoid duplicating derived server state in global client stores.

### UI Behavior

- The first screen of a feature should be the working interface, not a marketing or explanatory page.
- Use dense, operational UI for admin workflows.
- Do not add visible instructional text for obvious controls.
- Use icon buttons for common actions such as edit, delete, back, save, and close.
- Keep table/list rows and form controls stable in size to avoid layout shift.
- Make validation and loading states explicit.

### Localization

- User-facing labels are expected to be Ukrainian unless the existing client uses another language in that area.
- API enum values and request fields must remain server-compatible English values.
- Format dates for display locally, but send date-only API values as `YYYY-MM-DD`.

## Error Handling

The server commonly returns:

- `400` validation problem
- `403` forbidden
- `404` not found
- `409` conflict

Client behavior:

- Display field-level validation errors when available.
- Display conflict errors as actionable messages, for example duplicate group or invalid assignment.
- Do not silently swallow `403`; it usually means the selected entity does not belong to the secretary specialty or the secretary is inactive.
- Keep destructive actions behind a confirmation UI.

## Before Finishing Client Work

Run the relevant client checks available in the client project, for example:

```text
npm run lint
npm run typecheck
npm run build
```

If a local backend is needed, verify requests against the actual server rather than mocked assumptions.

For API-related changes, also inspect the matching server feature to confirm:

- Route
- HTTP method
- Query parameters
- Request body
- Response shape
- Error statuses
- Validation rules

## Do Not

- Do not send academic-year display values such as `2025/26` as `DefenseYear`.
- Do not convert date-only fields into date-time strings.
- Do not localize enum values inside API payloads.
- Do not assume a group can exist without a secretary specialty context.
- Do not assume a DEC can exist without at least one group.
- Do not add client-only business rules that contradict server validation.
