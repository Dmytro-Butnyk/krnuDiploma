import type { EntitySchema, EntitySchemaNode, SchemaPath } from '../../entities/schema/model/types'

export function getPathsForEntity(
  schema: EntitySchema,
  entityName: string,
  prefix = '',
  depth = 0,
  maxDepth = 2,
): SchemaPath[] {
  if (depth > maxDepth) return []

  const schemaNode: EntitySchemaNode | undefined = schema[entityName]
  if (!schemaNode) return []

  const scalarPaths = schemaNode.scalars.map((scalar) => ({
    fullPath: prefix ? `${prefix}.${scalar}` : scalar,
    isCollection: false as const,
  }))

  const entityPaths = Object.entries(schemaNode.entities).flatMap(([navProp, targetEntity]) => {
    const nextPrefix = prefix ? `${prefix}.${navProp}` : navProp
    return getPathsForEntity(schema, targetEntity, nextPrefix, depth + 1, maxDepth)
  })

  const collectionPaths = Object.entries(schemaNode.collections).map(([navProp, targetEntity]) => ({
    fullPath: prefix ? `${prefix}.${navProp}` : navProp,
    isCollection: true as const,
    targetType: targetEntity,
  }))

  return [...scalarPaths, ...entityPaths, ...collectionPaths]
}

export function getSourceArrayScalarPaths(
  schema: EntitySchema,
  dataSources: Array<{ Key: string; Entity: string }>,
  sourceArrayPath: string,
) {
  if (!sourceArrayPath) return []

  const [rootKey, ...rest] = sourceArrayPath.split('.')
  const dataSource = dataSources.find((source) => source.Key === rootKey)
  if (!dataSource) return []

  let targetEntityType = dataSource.Entity

  if (rest.length > 0) {
    const navPath = rest.join('.')
    const collectionPathInfo = getPathsForEntity(schema, dataSource.Entity).find(
      (path) => path.fullPath === navPath && path.isCollection,
    )

    if (collectionPathInfo?.isCollection) {
      targetEntityType = collectionPathInfo.targetType
    }
  }

  return getPathsForEntity(schema, targetEntityType)
    .filter((path) => !path.isCollection)
    .map((path) => path.fullPath)
}
