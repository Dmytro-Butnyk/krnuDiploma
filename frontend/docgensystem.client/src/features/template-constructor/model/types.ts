export type TagKind = 'scalar' | 'table_column' | 'reserved'
export type MappingMode = 'scalars' | 'tables'

export type FilterConditionType = 'arg' | 'const'
export type FilterOperator = '==' | '!=' | '.Contains'

export type NewDataSourceCondition = {
  property: string
  operator: FilterOperator
  type: FilterConditionType
  value: string
}

export type NewDataSourceDraft = {
  entity: string
  key: string
  conditions: NewDataSourceCondition[]
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
  Mapping: {
    Tables: Record<string, TableMappingConfig>
    Scalars: Record<string, string>
  }
  DataSources: DataSourceConfig[]
}

export type ConstructorStep = 1 | 2 | 3 | 4
