export type EntitySchemaNode = {
  scalars: string[]
  entities: Record<string, string>
  collections: Record<string, string>
  keyScalars: string[]
  foreignKeys: ForeignKeySchemaNode[]
  references: EntityReferenceSchemaNode[]
  displayCandidates: string[]
}

export type EntitySchema = Record<string, EntitySchemaNode>

export type ForeignKeySchemaNode = {
  property: string
  targetEntity: string
}

export type EntityReferenceSchemaNode = {
  navigation: string
  targetEntity: string
  foreignKeys: string[]
  isCollection: boolean
}

export type SchemaPath =
  | {
      fullPath: string
      isCollection: false
    }
  | {
      fullPath: string
      isCollection: true
      targetType: string
    }
