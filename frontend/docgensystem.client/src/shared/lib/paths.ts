import type { EntitySchema, EntitySchemaNode, SchemaPath } from '../../entities/schema/model/types'

const MAX_GENERATED_SCHEMA_PATHS = 5000
const pathsCache = new WeakMap<EntitySchema, Map<string, SchemaPath[]>>()

function addPath(paths: SchemaPath[], path: SchemaPath) {
  if (paths.length >= MAX_GENERATED_SCHEMA_PATHS) return false

  paths.push(path)
  return true
}

function buildPathsForEntity(
  schema: EntitySchema,
  entityName: string,
  visitedEntities: Set<string>,
  prefix = '',
): SchemaPath[] {
  if (visitedEntities.has(entityName)) return []

  const schemaNode: EntitySchemaNode | undefined = schema[entityName]
  if (!schemaNode) return []

  const nextVisitedEntities = new Set(visitedEntities)
  nextVisitedEntities.add(entityName)
  const paths: SchemaPath[] = []

  for (const scalar of schemaNode.scalars) {
    if (!addPath(paths, {
      fullPath: prefix ? `${prefix}.${scalar}` : scalar,
      isCollection: false,
    })) {
      return paths
    }
  }

  for (const [navProp, targetEntity] of Object.entries(schemaNode.entities)) {
    if (nextVisitedEntities.has(targetEntity)) continue

    const nextPrefix = prefix ? `${prefix}.${navProp}` : navProp
    const entityPaths = buildPathsForEntity(schema, targetEntity, nextVisitedEntities, nextPrefix)
    for (const path of entityPaths) {
      if (!addPath(paths, path)) return paths
    }
  }

  for (const [navProp, targetEntity] of Object.entries(schemaNode.collections)) {
    if (nextVisitedEntities.has(targetEntity)) continue
    if (!addPath(paths, {
      fullPath: prefix ? `${prefix}.${navProp}` : navProp,
      isCollection: true,
      targetType: targetEntity,
    })) {
      return paths
    }
  }

  return paths
}

export function getPathsForEntity(schema: EntitySchema, entityName: string): SchemaPath[] {
  let schemaCache = pathsCache.get(schema)
  if (!schemaCache) {
    schemaCache = new Map<string, SchemaPath[]>()
    pathsCache.set(schema, schemaCache)
  }

  const cachedPaths = schemaCache.get(entityName)
  if (cachedPaths) return cachedPaths

  const paths = buildPathsForEntity(schema, entityName, new Set())
  schemaCache.set(entityName, paths)
  return paths
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
