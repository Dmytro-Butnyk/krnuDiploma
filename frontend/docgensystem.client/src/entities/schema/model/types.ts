export type EntitySchemaNode = {
  scalars: string[]
  entities: Record<string, string>
  collections: Record<string, string>
}

export type EntitySchema = Record<string, EntitySchemaNode>

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
