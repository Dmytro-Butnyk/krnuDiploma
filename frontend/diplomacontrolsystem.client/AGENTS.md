# AGENTS.md

Guidance for AI agents working on the Diploma Control System client.

This repository folder is the React client application. Do not inspect or modify sibling backend projects unless the user explicitly asks for it. Use the OpenAPI document supplied by the user, such as `v1 (3).json`, plus this file as the contract source for client work.

## Project Shape

- Vite + React + TypeScript single-page application.
- Routing is handled by `react-router-dom`.
- Server state is handled by TanStack Query.
- Forms use local state or React Hook Form/Zod when already present or clearly useful.
- Styling uses Tailwind CSS utilities and the existing visual language in `src/index.css`.

## Product Decisions

- Authentication is temporary and email-based. The login screen stores a secretary email on the client and sends it to API calls that require `secretaryEmail`.
- Future authentication is expected to use Google OAuth. Keep the temporary secretary-email session isolated.
- The Generator tab redirects to an external client/site. Until the real URL is known, use `https://www.google.com`.
- The DEC is not a top-level navigation tab. It is part of the Groups workflow.
- There is one diploma examination commission for the selected `secretaryEmail + educationLevel + defenseYear`.
- Groups are automatically associated with that commission on the server. The client must not send group ids when creating or updating a DEC.

## API Base

All API routes are mounted under:

```text
/api
```

Use camelCase JSON body keys unless the endpoint requires multipart form data or the OpenAPI contract explicitly says otherwise.

## Main Client Areas

### Groups

- `GET /api/groups/academic-years`
- `POST /api/groups`
- `PATCH /api/groups/{groupId}`
- `DELETE /api/groups/{groupId}`
- `GET /api/groups/{groupId}/students`
- `GET /api/groups/{groupId}/statistics`

Group creation uses `multipart/form-data`.

When sending a Google Drive import value, include the multipart field expected by the current API contract. Preserve compatibility with the existing client behavior unless the OpenAPI spec is updated.

### Students

- `POST /api/groups/{groupId}/students`
- `GET /api/students/{studentId}/details`
- `DELETE /api/students/{studentId}`
- `GET /api/students/{studentId}/qualification-work-options`
- `PATCH /api/students/{studentId}/name`
- `PATCH /api/students/{studentId}/qualification-work`
- `PATCH /api/students/{studentId}/physical-checklist`
- `PATCH /api/students/{studentId}/electronic-checklist`
- `PATCH /api/students/{studentId}/defence`
- `PATCH /api/students/{studentId}/defence-results`
- `PATCH /api/students/{studentId}/qualification-work-characteristics`

For student PATCH endpoints, keep request and response types aligned with the OpenAPI schema.

### Diploma Examination Commission

- `GET /api/diploma-examination-commissions`
  - Query: `SecretaryEmail`, `EducationLevel`, `DefenseYear`.
  - Returns the single commission for the selected secretary, education level, and defense year.
- `GET /api/diploma-examination-commissions/options`
  - Query: `SecretaryEmail`, optional `CommissionId`.
  - Returns teachers, secretaries, and commission heads for the form.
- `POST /api/diploma-examination-commissions`
  - Creates the single commission.
  - Sends `educationLevel`, `defenseYear`, `secretaryEmail`, `secretaryId`, `orderNumber`, `commissionHeadId`, member teacher ids, `startDate`, and `endDate`.
  - Does not send group ids.
- `PUT /api/diploma-examination-commissions/{commissionId}`
  - Updates commission metadata, head, members, secretary, and dates.
  - Does not send group ids.
- `DELETE /api/diploma-examination-commissions/{commissionId}`
  - Deletes the commission without deleting archived defense data.

Commission heads are managed separately:

- `GET /api/commission-heads`
- `POST /api/commission-heads`
- `PUT /api/commission-heads/{commissionHeadId}`
- `DELETE /api/commission-heads/{commissionHeadId}`

When creating a commission head, collect `fullName`, `position`, `company`, and `specialty` from the user. This `specialty` belongs to the external commission head and is not the same specialty used for secretary scoping.

## Domain Rules

### Defense Year

The server stores the actual defense year.

```text
DefenseYear: 2026
Academic display year: 2025/26
```

Client forms must send the actual defense year, for example `2026`. Do not send academic display strings such as `2025/26` as year values.

### Education Level

API enum values are:

```text
Bachelor
Master
```

Keep API payload values in English. User-facing labels should be Ukrainian.

### Dates

Use ISO date-only strings:

```text
YYYY-MM-DD
```

Do not send date-time strings or time zones for date-only fields.

DEC `startDate` and `endDate` must belong to the selected `defenseYear`, and `endDate` must be greater than or equal to `startDate`.

## UI Behavior

- The first screen of a feature should be the working interface.
- Use dense operational UI for admin workflows.
- Keep Groups and DEC in one flow: the left panel lists groups and the commission block; the right panel shows either the selected group or the commission details.
- Use icon buttons for common compact actions where practical.
- Keep destructive actions behind a confirmation dialog.
- Show loading, empty, validation, and server-error states explicitly.
- User-facing labels are expected to be Ukrainian.

## State And Cache

- Load server state through TanStack Query.
- Invalidate group queries after group and student mutations.
- Invalidate commission queries after commission or commission-head mutations.
- Do not duplicate derived server state in global client stores.

## Error Handling

The API commonly returns:

- `400` validation problem
- `403` forbidden
- `404` not found
- `409` conflict

Show validation errors near relevant fields when available. Treat `403` as an access/specialty/inactive-secretary problem, not as a generic network failure.

For `GET /api/diploma-examination-commissions`, a `404` can mean no commission exists yet for the selected context; the UI should allow creating one.

## Before Finishing Work

Run the relevant client checks:

```text
npm run lint
npm run build
```

If a check cannot be run, report why.

## Do Not

- Do not add the DEC back as a top-level tab.
- Do not send group ids in DEC create/update requests.
- Do not send academic-year display values such as `2025/26` as `DefenseYear`.
- Do not convert date-only fields into date-time strings.
- Do not localize enum values inside API payloads.
- Do not silently swallow `403`.
- Do not modify sibling backend projects unless explicitly requested.
