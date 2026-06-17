# Frontend Statistics Implementation Guide

This document is the handoff for the frontend agent. Implement the updated statistics UI against the new split backend endpoints.

## Goal

Replace the old single statistics integration with four separate statistics views for a group:

1. Group defence results.
2. Previous year comparison.
3. Supervisor workload.
4. Practice base rating.

The backend no longer sends UI labels for common statistic sections/items. Render labels on the frontend by mapping stable backend keys.

## Implementation Tasks

1. Update the API client layer.
   Add separate request functions for each endpoint listed below. Do not expect `previousYearStatistics` inside the base group statistics response anymore.

2. Update the statistics navigation/tabs.
   The mockups show four pages/buttons:
   - defence results;
   - previous year comparison;
   - supervisor workload;
   - practice base rating.

3. Update the group statistics page.
   It should call only `GET /api/groups/{groupId}/statistics` and render sections from `sections`.

4. Implement the previous year comparison page.
   It should call `GET /api/groups/{groupId}/statistics/previous-year-comparison` and compare `currentGroup.sections` with `previousYear.sections`.

5. Implement the supervisor workload table.
   It should call `GET /api/groups/{groupId}/statistics/supervisor-workload`.

6. Implement the practice base rating table.
   It should call `GET /api/groups/{groupId}/statistics/practice-bases`.

7. Add frontend label dictionaries.
   Use the `section.key` and `item.key` values below. Backend intentionally does not return Ukrainian labels anymore.

8. Add empty/null states.
   Handle empty groups, missing previous year data, students without supervisors, and students without practice bases.

Backend split the old group statistics response into separate statistics features. All endpoints are under `/api` and require the existing secretary auth.

## Routes

### Group statistics

`GET /api/groups/{groupId}/statistics`

Use for the "results of defence" statistics page. This endpoint no longer contains previous-year data.

Response shape:

```ts
type GroupStatisticsResponse = {
  groupId: number;
  groupName: string;
  totalStudents: number;
  sections: StatisticSection[];
};
```

### Previous year comparison

`GET /api/groups/{groupId}/statistics/previous-year-comparison`

Use for the comparison page. The previous year data aggregates all groups from `currentGroup.defenseYear - 1` with the same specialty and education level.

Response shape:

```ts
type PreviousYearComparisonResponse = {
  groupId: number;
  groupName: string;
  currentGroup: StatisticsSnapshot;
  previousYear: StatisticsSnapshot | null;
};

type StatisticsSnapshot = {
  defenseYear: string;
  groupsCount: number;
  totalStudents: number;
  sections: StatisticSection[];
};
```

`previousYear` is `null` when the current group year is not numeric or there are no matching previous-year groups.

Frontend behavior:

- If `previousYear === null`, show a normal empty state for comparison data.
- Use `currentGroup.sections` for the current bars.
- Use `previousYear.sections` for the previous-year bars.
- Match section/item values by `key`, not by array index.

### Supervisor workload

`GET /api/groups/{groupId}/statistics/supervisor-workload`

Use for the supervisor workload table.

Response shape:

```ts
type SupervisorWorkloadResponse = {
  groupId: number;
  groupName: string;
  summary: {
    totalSupervisors: number;
    totalStudents: number;
  };
  items: SupervisorWorkloadItem[];
};

type SupervisorWorkloadItem = {
  key: "supervisor" | "withoutSupervisor";
  teacherId: number | null;
  fullName: string | null;
  shortName: string | null;
  studentsCount: number;
  averageScore: number | null;
  diplomasWithHonorsCount: number;
  averagePlagiarismPercent: number | null;
};
```

Rows with `key: "supervisor"` are sorted by `studentsCount` descending, then by `fullName`. If the group has students without a supervisor, the backend appends one extra row with `key: "withoutSupervisor"` and null teacher/average fields. `summary.totalStudents` counts all group students, including this row. `summary.totalSupervisors` counts only real teachers.

Frontend behavior:

- For `key: "supervisor"`, render teacher name from `shortName` or `fullName`.
- For `key: "withoutSupervisor"`, render a synthetic label such as `Студенти без керівника`.
- `averageScore` and `averagePlagiarismPercent` are nullable only for the synthetic row.
- Display `summary.totalSupervisors` and `summary.totalStudents` under the table.

### Practice base rating

`GET /api/groups/{groupId}/statistics/practice-bases`

Use for the practice base rating page.

Response shape:

```ts
type PracticeBaseRatingResponse = {
  groupId: number;
  groupName: string;
  totalStudents: number;
  totalPracticeBases: number;
  items: PracticeBaseRatingItem[];
};

type PracticeBaseRatingItem = {
  key: "practiceBase" | "withoutPracticeBase";
  rank: number | null;
  practiceBase: string | null;
  studentsCount: number;
};
```

Rows with `key: "practiceBase"` are sorted by `studentsCount` descending, then by `practiceBase`. The backend appends `key: "withoutPracticeBase"` when some students have empty practice base. That special row has `rank: null` and `practiceBase: null`.

Frontend behavior:

- For `key: "practiceBase"`, render `rank`, `practiceBase`, and `studentsCount`.
- For `key: "withoutPracticeBase"`, render a synthetic label such as `Студенти без бази практики`.
- The synthetic row has no rank; keep it visually distinct from ranked rows if needed.

## Shared Statistics DTO

Group statistics and previous-year snapshots use the same dry structure:

```ts
type StatisticSection = {
  key: StatisticSectionKey;
  items: StatisticItem[];
};

type StatisticItem = {
  key: StatisticItemKey;
  count: number;
  percentage: number;
};
```

The backend no longer sends display labels or section titles. Frontend components should map labels by `section.key` and `item.key`.

Suggested label maps:

```ts
export const statisticSectionLabels: Record<StatisticSectionKey, string> = {
  gradesAndRecommendations: "Оцінки ЕК та рекомендації",
  workCharacter: "Характер виконання дипломних проєктів та робіт",
  complexDiplomaDesign: "Комплексне дипломне проєктування",
  additional: "Додатково",
  performanceIndicators: "Показники якості та успішності",
};

export const statisticItemLabels: Record<StatisticItemKey, string> = {
  excellent: "Відмінно",
  good: "Добре",
  satisfactory: "Задовільно",
  diplomaWithHonors: "Диплом з відзнакою",
  recommendedForMaster: "Рекомендовано в магістратуру",
  researchBased: "Дослідного характеру",
  realProjects: "З реальними проєктами та конструкторсько-технологічними розробками",
  ecoFriendly: "З раціонального природовикористання, ресурсозбереження та охорони навколишнього середовища",
  enterpriseOrdered: "За замовленням підприємства",
  interuniversity: "Міжвузівські",
  interdepartmental: "Міжкафедральні",
  departmental: "Кафедральні",
  complexProjectParticipant: "Студенти, які брали участь у комплексному проєкті",
  recommendedForImplementation: "До впровадження",
  defendedAtEnterprise: "Захищено на підприємстві",
  educationQuality: "Якість навчання",
  overallSuccess: "Загальна успішність",
};
```

Section keys:

```ts
type StatisticSectionKey =
  | "gradesAndRecommendations"
  | "workCharacter"
  | "complexDiplomaDesign"
  | "additional"
  | "performanceIndicators";
```

Item keys:

```ts
type StatisticItemKey =
  | "excellent"
  | "good"
  | "satisfactory"
  | "diplomaWithHonors"
  | "recommendedForMaster"
  | "researchBased"
  | "realProjects"
  | "ecoFriendly"
  | "enterpriseOrdered"
  | "interuniversity"
  | "interdepartmental"
  | "departmental"
  | "complexProjectParticipant"
  | "recommendedForImplementation"
  | "defendedAtEnterprise"
  | "educationQuality"
  | "overallSuccess";
```

Percentages are rounded by backend to one decimal place. Empty groups return counts `0` and percentages `0`.

## Suggested Page Mapping

Tabs/buttons from the mockups can call:

- Results: `/statistics`
- Previous year comparison: `/statistics/previous-year-comparison`
- Supervisor workload: `/statistics/supervisor-workload`
- Practice base rating: `/statistics/practice-bases`

The group name comes from each response as `groupName`; no extra group fetch is needed for page titles.

## Acceptance Checklist

- The base statistics page does not read `label`, `title`, or `previousYearStatistics`.
- Previous-year comparison handles `previousYear: null`.
- Comparison pairs values by section/item key.
- Supervisor workload renders both real supervisors and the optional `withoutSupervisor` row.
- Practice base rating renders ranked bases and the optional `withoutPracticeBase` row.
- All four pages show `groupName` from the endpoint response.
- Percentages are displayed from backend values as-is; frontend should not recalculate them.
