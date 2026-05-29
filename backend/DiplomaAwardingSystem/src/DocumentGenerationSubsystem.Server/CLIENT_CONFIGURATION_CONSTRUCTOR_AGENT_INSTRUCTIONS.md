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

The mapping editor must allow two root types:

- `Input.SomeKey`
- `SomeDataSourceKey.Property.Path`

Examples:

```json
"OrderDate": "Input.OrderDate"
```

```json
"Specialty": "TargetStudent.Group.Specialty.Name"
```

The `Input` root is reserved by the server. Do not allow a `DataSource.Key` named `Input`.

## Inputs

Support two input kinds.

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

## DataSources

Every `DataSource.FilterArgs` value must point to an existing input key.

Example for one selected student:

```json
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
```

Example for a selected group:

```json
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
```

`Includes` are only for generation data. Do not use generation `Includes` to load select options.

## Mapping

Scalar mapping:

```json
"Scalars": {
  "OrderDate": "Input.OrderDate",
  "Specialty": "TargetStudent.Group.Specialty.Name"
}
```

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

## Generation Form Runtime

For the actual "generate document" form, do not parse the raw configuration JSON on the client.

Use:

```http
GET /api/documents/templates/{id}/generation-form
```

This endpoint returns normalized input metadata, including `OptionsEndpoint` for entity selects.

Client behavior:

- render `Manual` by `ValueType`;
- render `EntitySelect` as select/autocomplete;
- respect `Required`;
- if an input has `DependsOn`, disable it until all dependencies have values;
- when a dependency changes, clear dependent input values.

## Entity Options Runtime

Never load all database rows on the client.

For every `EntitySelect`, call its options endpoint lazily:

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

## Client-Side Validation Before Save

Before upload/update, validate at least:

- `ConfigurationVersion` is `2`;
- input keys are unique, non-empty, and stable;
- no input key is `Input`;
- every `Input.Kind` is supported;
- every `Input.ValueType` is supported;
- every `EntitySelect.Entity` exists in constructor schema;
- every `DependsOn` item points to an existing input;
- every filter `Input` points to an existing input;
- every filter operator is `Equals`;
- every `DataSource.Key` is unique and not `Input`;
- every `DataSource.Entity` exists in constructor schema;
- every `DataSource.FilterArgs` item points to an existing input;
- every mapping root is either `Input` or an existing `DataSource.Key`;
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
5. User creates data sources using selected inputs as `FilterArgs`.
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
