# Client Configuration Constructor Agent Instructions

## Goal

Rework the client-side document configuration constructor to create the new document-generation configuration format for `DocumentGenerationSubsystem.Api`.

Use these instructions together with the server OpenAPI documentation. The OpenAPI spec is the source of exact routes and response shapes; this document explains how the client should think about the configuration model and UI behavior.

## Version Policy

The system does not support multiple active configuration formats.

Every newly created or saved configuration must include:

```json
"ConfigurationVersion": 2
```

Treat this as a version gate, not as backwards-compatible versioning:

- client creates only the current version;
- server accepts only the current version;
- if an existing template has no `ConfigurationVersion` or has an unsupported version, the client should block editing/generation and tell the user that the template configuration must be rebuilt or migrated;
- do not build several constructors for several versions.

## Configuration Shape

The client must save JSON in this shape:

```json
{
  "ConfigurationVersion": 2,
  "Inputs": {},
  "DataSources": [],
  "Mapping": {
    "Scalars": {},
    "Tables": {}
  }
}
```

Conceptually:

- `Inputs` describes what the user must enter or select before generation.
- `DataSources` describes what the server loads from the database.
- `Mapping` describes how `Inputs` and `DataSources` are written into MiniWord tags.

## Mapping Roots

The mapping editor must allow three root types:

- `Input.SomeKey`
- `Computed.SomeKey`
- `SomeDataSourceKey.Property.Path`

Examples:

```json
"OrderDate": "Input.OrderDate"
```

```json
"ProtocolsNumbers": "Computed.ProtocolsNumbers"
```

```json
"Specialty": "TargetStudent.Group.Specialty.Name"
```

The `Input` and `Computed` roots are reserved by the server. Do not allow a `DataSource.Key` named `Input` or `Computed`.

## Inputs

Support three input kinds.

### Manual

Use `Manual` when the value does not come from the database.

Examples:

- order date;
- order number;
- custom document note;
- secretary-entered text;
- arbitrary boolean/number/date needed by a template.

Example:

```json
"OrderDate": {
  "Kind": "Manual",
  "ValueType": "Date",
  "Label": "Дата наказу",
  "Required": true
}
```

Example with max length:

```json
"OrderNumber": {
  "Kind": "Manual",
  "ValueType": "String",
  "Label": "Номер наказу",
  "Required": true,
  "MaxLength": 50
}
```

### EntitySelect

Use `EntitySelect` when the user must select a row from the database.

Example:

```json
"GroupId": {
  "Kind": "EntitySelect",
  "Entity": "Group",
  "ValueType": "Int",
  "Label": "Група",
  "Required": true,
  "Display": ["Name", "Year"],
  "Search": ["Name"],
  "OrderBy": ["Year desc", "Name"]
}
```

Example with dependency:

```json
"StudentId": {
  "Kind": "EntitySelect",
  "Entity": "Student",
  "ValueType": "Int",
  "Label": "Студент",
  "Required": true,
  "DependsOn": ["GroupId"],
  "Filters": [
    {
      "Property": "GroupId",
      "Operator": "Equals",
      "Input": "GroupId"
    }
  ],
  "Display": ["FullName"],
  "Search": ["FullName"],
  "OrderBy": ["FullName"]
}
```

Supported `ValueType` values:

- `String`
- `Int`
- `Long`
- `Guid`
- `Bool`
- `Date`
- `DateTime`
- `Decimal`

Most current entity IDs are `Int`.

### ValueSelect

Use `ValueSelect` when the user must select a distinct scalar value from the database instead of an entity `Id`.

This is mainly for dependent form fields such as "select a group, then select one of the defence dates that actually exists for that group".

Example:

```json
"DefenceDate": {
  "Kind": "ValueSelect",
  "Entity": "QualificationWork",
  "ValueType": "Date",
  "ValuePath": "DefenceDate",
  "Label": "Дата захисту",
  "Required": true,
  "DependsOn": ["GroupId"],
  "Filters": [
    {
      "Property": "Student.GroupId",
      "Operator": "Equals",
      "Input": "GroupId"
    }
  ],
  "OrderBy": ["DefenceDate"]
}
```

The options endpoint returns scalar values, not entity IDs. For `Date`, submit the selected value back as an ISO date string such as `2022-06-13`.

## Constructor Schema

Load database schema from:

```http
GET /api/constructor/schema
```

Use it to build the constructor UI.

Important schema fields:

- `scalars`: scalar fields available for mapping.
- `entities`: single navigation properties.
- `collections`: collection navigation properties available for table mappings.
- `keyScalars`: primary key fields.
- `foreignKeys`: FK fields and their target entity.
- `references`: navigation metadata, target entity, FK fields, and whether the navigation is a collection.
- `displayCandidates`: recommended fields for select labels.

The client should use `foreignKeys` and `references` to suggest dependent inputs.

Example rule:

- User creates `StudentId` for entity `Student`.
- Schema says `Student` has FK `GroupId` to `Group`.
- Client suggests adding `GroupId` input first.
- If the user accepts, configure `StudentId.DependsOn = ["GroupId"]` and a filter where `Property = "GroupId"` and `Input = "GroupId"`.

Do not silently force parent filters in every case. Some templates may intentionally allow selecting from the full entity list.

## Constructor Scenarios

The constructor should support two data setup modes:

- `Manual setup`: keep the existing step-by-step data source UI.
- `Scenario`: apply a predefined server-provided configuration fragment, then let the user continue with tag mapping.

Load available scenarios from:

```http
GET /api/constructor/scenarios
```

Each scenario returns:

- `id`, `title`, `description`;
- `inputs`: inputs to merge into the current configuration;
- `dataSources`: data sources to merge into the current configuration;
- `recommendedTableSources`: table source hints for the mapping UI;
- `requiredScalarMappings`: scenario-required scalar tags and the exact mapping paths they must use;
- `requiredTableSources`: scenario-required table source arrays that must be used by at least one table mapping.

Scenarios are hardcoded server-side for now. Do not persist them separately on the client. The saved template still stores a normal `ConfigurationVersion: 2` JSON configuration.

Initial scenario:

- `group-defence-day-extract` / `Витяг за день захисту групи`
  - creates `GroupId`;
  - creates dependent `DefenceDate` as `ValueSelect`;
  - creates `TargetGroup`;
  - creates `DayStudents` filtered by selected group, selected defence date, and `QualificationWork.CommissionScore >= 60`;
  - exposes computed scalar `Computed.ProtocolsNumbers`, calculated from group defence order and students with `CommissionScore >= 60`;
  - recommends `DayStudents` for table mappings.

The scenario must not create manual input fields for arbitrary document text such as commission number, rector name, etc. Those tags are still configured by the user through the normal scalar manual-input mapping flow. `DefenceDate` is the exception because it is both displayed in the document and required for filtering `DayStudents`. `ProtocolsNumbers` is also not manual: it must be mapped to `Computed.ProtocolsNumbers`.

For `group-defence-day-extract`, the client must enforce:

- the uploaded template must contain scalar tag `{{DefenceDate}}`;
- the uploaded template must contain scalar tag `{{ProtocolsNumbers}}`;
- `Mapping.Scalars.DefenceDate` must be `Input.DefenceDate`;
- `Mapping.Scalars.ProtocolsNumbers` must be `Computed.ProtocolsNumbers`;
- at least one table mapping must use `SourceArray: "DayStudents"`;
- the UI must warn/block if the user maps the student table to `TargetGroup.Students`, because that bypasses the scenario filter by defence date and score.

If a scenario-required tag is missing from the scanned template, block scenario application and show a clear message telling the user to edit the `.docx` template and add the required tag. Do not silently create manual input duplicates such as `DefenceDate2` or manual `ProtocolsNumbers`.

When applying required scalar mappings, auto-map them and lock them in the UI. The user should see that these are scenario-controlled fields, but should not be able to change their mapping because doing so breaks the scenario.

When the user selects a scenario:

1. Merge scenario `inputs` into `Inputs`.
2. Merge scenario `dataSources` into `DataSources`.
3. Skip manual data-source construction unless the user chooses to edit manually.
4. Continue to the scalar/table mapping step.
5. Keep ordinary tag mapping flexible. Only scenario-required tags from `requiredScalarMappings` and table sources from `requiredTableSources` are fixed and locked.

## DataSources

Every `DataSource.FilterArgs` value must point to an existing input key.

Example for one selected student:

```json
{
  "Key": "TargetStudent",
  "Entity": "Student",
  "Result": "One",
  "Filter": "Id == @0",
  "FilterArgs": ["StudentId"],
  "Includes": [
    "Group.Specialty",
    "QualificationWork.Teacher"
  ],
  "OrderBy": []
}
```

Example for a selected group:

```json
{
  "Key": "TargetGroup",
  "Entity": "Group",
  "Result": "One",
  "Filter": "Id == @0",
  "FilterArgs": ["GroupId"],
  "Includes": [
    "Specialty",
    "Students.QualificationWork.Teacher"
  ],
  "OrderBy": []
}
```

`Includes` are only for generation data. Do not use generation `Includes` to load select options.

`Result` is optional and defaults to `One` for old configurations. Use:

- `One`: load one object, equivalent to the old behavior;
- `Many`: load a list for table mapping.

Use `OrderBy` when `Result` is `Many` so generated table rows are stable.

Example for a scenario-provided list:

```json
{
  "Key": "DayStudents",
  "Entity": "Student",
  "Result": "Many",
  "Filter": "GroupId == @0 && QualificationWork.DefenceDate == @1 && QualificationWork.CommissionScore >= 60",
  "FilterArgs": ["GroupId", "DefenceDate"],
  "Includes": [
    "Group.Specialty",
    "QualificationWork"
  ],
  "OrderBy": ["FullName"]
}
```

## Mapping

Scalar mapping:

```json
"Scalars": {
  "OrderDate": "Input.OrderDate",
  "ProtocolsNumbers": "Computed.ProtocolsNumbers",
  "Specialty": "TargetStudent.Group.Specialty.Name"
}
```

`Computed.ProtocolsNumbers` is a server-calculated scalar for the `group-defence-day-extract` scenario. It is not submitted by the user. The server calculates it by ordering all students in the selected group with `QualificationWork.CommissionScore >= 60` by `QualificationWork.DefenceDate`, then `FullName`, and returning the protocol number range for the selected `DefenceDate`.

Table mapping for a group:

```json
"Tables": {
  "Student": {
    "SourceArray": "TargetGroup.Students",
    "RowMapping": {
      "StudentName": "FullName",
      "Topic": "QualificationWork.Topic",
      "TeacherShort": "QualificationWork.Teacher.ShortName"
    }
  }
}
```

Table mapping for one selected student:

```json
"Tables": {
  "Student": {
    "SourceArray": "TargetStudent",
    "RowMapping": {
      "StudentName": "FullName",
      "Topic": "QualificationWork.Topic"
    }
  }
}
```

MiniWord detail: tables are rendered from a list of dictionaries. The server intentionally treats a single object as a one-row table, so `SourceArray: "TargetStudent"` is valid for a one-student table.

Avoid nested list mappings. The server rejects nested collections because MiniWord does not support arbitrary nested lists in this flow.

The server applies Ukrainian document formatting for known field paths. Do not ask the user to choose these formatters manually in the constructor.

Current automatic formatters:

- `QualificationWork.NationalGrade`: `відмінно`, `добре`, `задовільно`;
- `QualificationWork.HasDiplomaWithHonors`: `з відзнакою`, `без відзнаки`;
- `Group.EducationLevel`: `бакалавр`, `магістр`;
- date values are rendered as `dd.MM.yyyy` in generated documents.

## Generation Form Runtime

For the actual "generate document" form, do not parse the raw configuration JSON on the client.

Use:

```http
GET /api/documents/templates/{id}/generation-form
```

This endpoint returns normalized input metadata, including `OptionsEndpoint` for entity and value selects.

Client behavior:

- render `Manual` by `ValueType`;
- render `EntitySelect` as select/autocomplete;
- render `ValueSelect` as select/autocomplete using its `OptionsEndpoint`;
- respect `Required`;
- if an input has `DependsOn`, disable it until all dependencies have values;
- when a dependency changes, clear dependent input values.

## Entity Options Runtime

Never load all database rows on the client.

For every `EntitySelect` and `ValueSelect`, call its options endpoint lazily:

```http
GET /api/documents/templates/{templateId}/generation-inputs/{inputKey}/options
```

Query parameters:

- `q`: optional search string;
- `take`: optional page size;
- dependency values by input key, for example `GroupId=5`.

Example:

```http
GET /api/documents/templates/12/generation-inputs/StudentId/options?GroupId=5&q=іван&take=30
```

The server:

- applies filters from the saved configuration;
- validates dependency values;
- limits the number of rows;
- returns only option DTOs.

The client:

- passes all selected dependencies listed in `DependsOn`;
- debounces search input;
- does not request dependent options until dependencies are selected;
- displays `label` and optional `description`;
- stores/submits `value` as the generation parameter.

## Generate Request

When the user clicks generate, send all selected/input values:

```json
{
  "Parameters": {
    "GroupId": "5",
    "StudentId": "17",
    "OrderDate": "2026-05-29",
    "OrderNumber": "42"
  }
}
```

All values are sent as strings. The server parses them according to `Inputs.ValueType`.

Do not submit computed values in `Parameters`. For example, `Computed.ProtocolsNumbers` is calculated by the server from `GroupId` and `DefenceDate`, so the generate request for `group-defence-day-extract` should include `GroupId` and `DefenceDate`, but not `ProtocolsNumbers`.

## Client-Side Validation Before Save

Before upload/update, validate at least:

- `ConfigurationVersion` is `2`;
- input keys are unique, non-empty, and stable;
- no input key is `Input`;
- every `Input.Kind` is supported;
- every `Input.ValueType` is supported;
- every `EntitySelect.Entity` exists in constructor schema;
- every `ValueSelect` defines `Entity`, `ValuePath`, and `ValueType`;
- every `DependsOn` item points to an existing input;
- every filter `Input` points to an existing input;
- every filter operator is `Equals`;
- every `DataSource.Key` is unique and not `Input`;
- every `DataSource.Entity` exists in constructor schema;
- every `DataSource.FilterArgs` item points to an existing input;
- every `DataSource.Result`, when present, is `One` or `Many`;
- every mapping root is either `Input`, `Computed`, or an existing `DataSource.Key`;
- visible ID parameters are rendered as entity selects, not plain text boxes;
- table `SourceArray` points to a collection path or to a single object intentionally used as a one-row table.

Server validation remains authoritative. Client validation is for UX only.

## Recommended Constructor Workflow

1. User uploads/scans a `.docx` template and receives tags.
2. Client loads `/api/constructor/schema`.
3. User creates inputs:
   - manual fields for non-database tags;
   - entity selects for DB-driven parameters.
4. When user creates an entity select, client checks schema FK/reference metadata and suggests parent filters.
5. User creates data sources using selected inputs as `FilterArgs`, or applies a server-provided scenario and lets it create inputs/data sources.
6. User maps scalar tags from `Input.*` or data source paths.
7. User maps table tags from collection paths or one selected object.
8. Client validates config.
9. Client saves the template.
10. During generation, client uses `/generation-form` and options endpoints, not local JSON parsing.

## Example: Group Template

```json
{
  "ConfigurationVersion": 2,
  "Inputs": {
    "GroupId": {
      "Kind": "EntitySelect",
      "Entity": "Group",
      "ValueType": "Int",
      "Label": "Група",
      "Required": true,
      "Display": ["Name", "Year"],
      "Search": ["Name"],
      "OrderBy": ["Year desc", "Name"]
    },
    "OrderDate": {
      "Kind": "Manual",
      "ValueType": "Date",
      "Label": "Дата наказу",
      "Required": true
    }
  },
  "DataSources": [
    {
      "Key": "TargetGroup",
      "Entity": "Group",
      "Filter": "Id == @0",
      "FilterArgs": ["GroupId"],
      "Includes": [
        "Specialty",
        "Students.QualificationWork.Teacher"
      ]
    }
  ],
  "Mapping": {
    "Scalars": {
      "OrderDate": "Input.OrderDate",
      "Specialty": "TargetGroup.Specialty.Name",
      "SpecialtyNumber": "TargetGroup.Specialty.Code"
    },
    "Tables": {
      "Student": {
        "SourceArray": "TargetGroup.Students",
        "RowMapping": {
          "StudentName": "FullName",
          "Topic": "QualificationWork.Topic",
          "TeacherShort": "QualificationWork.Teacher.ShortName"
        }
      }
    }
  }
}
```

## Example: One Student Template Filtered By Group

```json
{
  "ConfigurationVersion": 2,
  "Inputs": {
    "GroupId": {
      "Kind": "EntitySelect",
      "Entity": "Group",
      "ValueType": "Int",
      "Label": "Група",
      "Required": true,
      "Display": ["Name", "Year"],
      "Search": ["Name"],
      "OrderBy": ["Year desc", "Name"]
    },
    "StudentId": {
      "Kind": "EntitySelect",
      "Entity": "Student",
      "ValueType": "Int",
      "Label": "Студент",
      "Required": true,
      "DependsOn": ["GroupId"],
      "Filters": [
        {
          "Property": "GroupId",
          "Operator": "Equals",
          "Input": "GroupId"
        }
      ],
      "Display": ["FullName"],
      "Search": ["FullName"],
      "OrderBy": ["FullName"]
    }
  },
  "DataSources": [
    {
      "Key": "TargetStudent",
      "Entity": "Student",
      "Filter": "Id == @0",
      "FilterArgs": ["StudentId"],
      "Includes": [
        "Group.Specialty",
        "QualificationWork.Teacher"
      ]
    }
  ],
  "Mapping": {
    "Scalars": {
      "Specialty": "TargetStudent.Group.Specialty.Name",
      "SpecialtyNumber": "TargetStudent.Group.Specialty.Code"
    },
    "Tables": {
      "Student": {
        "SourceArray": "TargetStudent",
        "RowMapping": {
          "StudentName": "FullName",
          "Topic": "QualificationWork.Topic",
          "TeacherShort": "QualificationWork.Teacher.ShortName"
        }
      }
    }
  }
}
```
