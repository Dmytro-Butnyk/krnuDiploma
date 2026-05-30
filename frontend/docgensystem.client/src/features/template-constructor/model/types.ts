export type TagKind = 'db_scalar' | 'input_scalar' | 'table_column' | 'reserved'
export type MappingMode = 'scalars' | 'tables'

export type InputKind = 'Manual' | 'EntitySelect'
export type InputValueType = 'String' | 'Int' | 'Long' | 'Guid' | 'Bool' | 'Date' | 'DateTime' | 'Decimal'
export type EntitySelectFilterOperator = 'Equals'
export type DataSourceFilterOperator = 'Equals' | 'NotEquals' | 'Contains'

export type InputFilterConfig = {
  Property: string
  Operator: EntitySelectFilterOperator
  Input: string
}

export type ManualInputConfig = {
  Kind: 'Manual'
  ValueType: InputValueType
  Label: string
  Required: boolean
  MaxLength?: number
}

export type EntitySelectInputConfig = {
  Kind: 'EntitySelect'
  Entity: string
  ValueType: InputValueType
  Label: string
  Required: boolean
  DependsOn?: string[]
  Filters?: InputFilterConfig[]
  Display?: string[]
  Description?: string[]
  Search?: string[]
  OrderBy?: string[]
}

export type InputConfig = ManualInputConfig | EntitySelectInputConfig

export type NewInputDraft = {
  key: string
  kind: InputKind
  entity: string
  valueType: InputValueType
  label: string
  required: boolean
  maxLength: string
  display: string[]
  description: string[]
  search: string[]
  orderBy: string[]
  dependsOn: string[]
  filters: InputFilterConfig[]
}

export type NewDataSourceDraft = {
  entity: string
  key: string
  inputKey: string
  filterProperty: string
  filterOperator: DataSourceFilterOperator
  argumentLabel: string
  parentFilterProperties: string[]
}

export type DataSourceConfig = {
  Key: string
  Entity: string
  Filter: string | null
  FilterArgs: string[]
  Includes: string[]
}

export type TableMappingConfig = {
  SourceArray: string
  RowMapping: Record<string, string>
}

export type TemplateConfiguration = {
  ConfigurationVersion: 2
  Inputs: Record<string, InputConfig>
  Mapping: {
    Tables: Record<string, TableMappingConfig>
    Scalars: Record<string, string>
  }
  DataSources: DataSourceConfig[]
}

export type ConstructorStep = 1 | 2 | 3 | 4
