# Frontend integration notes

## Person name forms

Student, teacher, and commission head responses now include `nameForms`:

```json
{
  "nominative": "Матченко Сергій Олександрович",
  "genitive": "Матченка Сергія Олександровича",
  "dative": "Матченку Сергію Олександровичу",
  "signature": "Сергій МАТЧЕНКО"
}
```

Requests accept `nameForms` as optional. If omitted, backend fills all cases from the current full name and signature from `shortName` where available.

Update screens:

- Student details/name edit: show and edit `nameForms`.
- Teacher management: show and edit `nameForms`.
- Commission head create/update: show and edit `nameForms`.
- Group students list now also returns `nameForms` for quick review.

## Academic degrees and teacher positions

Academic degree DTO/request now has:

```json
{
  "fullName": "...",
  "shortName": "...",
  "genitiveFullName": "...",
  "genitiveShortName": "..."
}
```

Teacher position DTO/request has the same two genitive fields.

If genitive fields are empty or omitted, backend copies `fullName` / `shortName`.

## Supervisor and reviewer selection

`GET /api/students/{studentId}/qualification-work-options` now returns:

```json
{
  "teachers": [],
  "supervisors": [],
  "reviewers": []
}
```

All three arrays contain all active teachers. Prefer using `teachers` in new UI. `supervisors` and `reviewers` remain for backward compatibility.

Backend no longer enforces specialty boundary:

- supervisor can be any active teacher;
- reviewer can be any active teacher;
- supervisor and reviewer still cannot be the same teacher.

## Defence questions

New endpoint:

```http
PATCH /api/students/{studentId}/qualification-work/defence-questions
```

Request:

```json
{
  "questions": [
    {
      "askedBy": "Ігор ШЕВЧЕНКО",
      "text": "Питання..."
    }
  ]
}
```

Rules:

- max 5 questions;
- `text` is required, max 1000;
- `askedBy` is optional, max 256.

`GET /api/students/{studentId}/details` returns questions under `qualificationWork.defenceQuestions`.

## Document scenarios

Template configuration now supports optional:

```json
{
  "ScenarioCode": "single-qualification-work-protocol"
}
```

Constructor scenarios are loaded from:

```http
GET /api/constructor/scenarios
```

The response includes `helperKeys`. Frontend should display scenarios as before, but when creating configuration from a scenario, it must copy scenario `id` into template config `ScenarioCode`.

Seeded scenario codes:

- `group-defence-day-extract`
- `single-qualification-work-protocol`

For the protocol scenario, useful computed paths include:

```text
Computed.StudentNameNominative
Computed.StudentNameGenitive
Computed.StudentNameDative
Computed.StudentSignatureName
Computed.SupervisorLine
Computed.ReviewerLine
Computed.CommissionHeadPresentLine
Computed.CommissionHeadSignatureName
Computed.FirstMemberPresentLine
Computed.SecondMemberPresentLine
Computed.ThirdMemberPresentLine
Computed.FirstMemberSignatureName
Computed.SecondMemberSignatureName
Computed.ThirdMemberSignatureName
Computed.SecretarySignatureName
Computed.DefenceQuestions
```

For the questions table use source array:

```text
Computed.DefenceQuestions
```

and row paths:

```text
Number
AskedBy
Text
```
