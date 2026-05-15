import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'
import type { EntitySchema } from '../../../entities/schema/model/types'
import type {
  ConstructorStep,
  DataSourceConfig,
  MappingMode,
  NewDataSourceCondition,
  NewDataSourceDraft,
  TagKind,
  TemplateConfiguration,
} from './types'

const defaultCondition: NewDataSourceCondition = {
  property: 'Id',
  operator: '==',
  type: 'arg',
  value: 'StudentId',
}

const initialConfig: TemplateConfiguration = {
  Mapping: { Tables: {}, Scalars: {} },
  DataSources: [],
}

export function getTableTagPrefix(tag: string) {
  const [prefix, ...rest] = tag.split('.')
  return rest.length > 0 ? prefix : null
}

export function getTableRowTagName(tag: string) {
  const [, ...rest] = tag.split('.')
  return rest.length > 0 ? rest.join('.') : tag
}

function isReservedNumberTag(tag: string) {
  return tag === 'Number' || tag.endsWith('.Number')
}

function normalizeRowMapping(rowMapping: Record<string, string>) {
  return Object.entries(rowMapping).reduce<Record<string, string>>((acc, [tag, path]) => {
    const rowTag = getTableRowTagName(tag)
    if (!(rowTag in acc) || tag === rowTag) {
      acc[rowTag] = path
    }
    return acc
  }, {})
}

function normalizeConfiguration(config: TemplateConfiguration): TemplateConfiguration {
  return {
    ...config,
    Mapping: {
      ...config.Mapping,
      Tables: Object.fromEntries(
        Object.entries(config.Mapping.Tables).map(([tableName, table]) => [
          tableName,
          { ...table, RowMapping: normalizeRowMapping(table.RowMapping) },
        ]),
      ),
    },
    DataSources: config.DataSources.map((source) => ({
      ...source,
      Includes: [...source.Includes],
    })),
  }
}

function getUniqueTableName(baseName: string, tables: Record<string, unknown>) {
  if (!tables[baseName]) return baseName

  let index = 2
  while (tables[`${baseName}_${index}`]) {
    index += 1
  }

  return `${baseName}_${index}`
}

function compactIncludes(includes: string[]) {
  return includes.filter(
    (include) => !includes.some((candidate) => candidate !== include && candidate.startsWith(`${include}.`)),
  )
}

type StoreState = {
  currentStep: ConstructorStep
  mappingMode: MappingMode
  selectedTag: string | null
  selectedTable: string | null
  newTableName: string
  newColumnTag: string
  newColumnPath: string
  searchQuery: string
  expandedSources: Record<string, boolean>
  tagTypes: Record<string, TagKind>
  newSource: NewDataSourceDraft
  config: TemplateConfiguration
  constructorSessionKey: string | null
  initialize: (payload: {
    tags: string[]
    config?: TemplateConfiguration
    defaultEntity?: string
    sessionKey: string
  }) => void
  hydrateTags: (tags: string[]) => void
  setStep: (step: ConstructorStep) => void
  nextStep: () => void
  previousStep: () => void
  setMappingMode: (mode: MappingMode) => void
  setSelectedTag: (tag: string | null) => void
  setSelectedTable: (tableName: string | null) => void
  setNewTableName: (name: string) => void
  setNewColumnTag: (tag: string) => void
  setNewColumnPath: (path: string) => void
  setSearchQuery: (query: string) => void
  setTagType: (tag: string, type: TagKind) => void
  updateNewSource: (patch: Partial<NewDataSourceDraft>) => void
  updateNewSourceCondition: (index: number, patch: Partial<NewDataSourceCondition>) => void
  addNewSourceCondition: () => void
  removeNewSourceCondition: (index: number) => void
  validateNewDataSource: () => { ok: true } | { ok: false; reason: string }
  addDataSource: () => { ok: true } | { ok: false; reason: string }
  removeDataSource: (key: string) => void
  toggleExpanded: (key: string) => void
  mapScalar: (tag: string, fullPath: string) => void
  unmapScalar: (tag: string) => void
  createNewTable: () => void
  deleteTable: (tableName: string) => void
  renameTable: (oldName: string, newName: string) => void
  updateTableSourceArray: (tableName: string, sourceArray: string) => void
  addColumnToTable: () => void
  removeColumnFromTable: (tableName: string, tag: string) => void
  calculateIncludes: (schema?: EntitySchema) => void
  reset: () => void
}

function buildDataSource(draft: NewDataSourceDraft): DataSourceConfig {
  const filterParts: string[] = []
  const filterArgs: string[] = []
  let argIndex = 0

  draft.conditions.forEach((condition) => {
    if (!condition.value) return

    if (condition.type === 'arg') {
      filterParts.push(`${condition.property} ${condition.operator} @${argIndex}`)
      filterArgs.push(condition.value)
      argIndex += 1
      return
    }

    if (condition.operator === '.Contains') {
      filterParts.push(`${condition.property}.Contains("${condition.value}")`)
      return
    }

    filterParts.push(`${condition.property} ${condition.operator} "${condition.value}"`)
  })

  return {
    Key: draft.key.trim(),
    Entity: draft.entity,
    Filter: filterParts.length > 0 ? filterParts.join(' AND ') : null,
    FilterArgs: filterArgs,
    Includes: [],
  }
}

function validateNewDataSourceDraft(draft: NewDataSourceDraft) {
  if (!draft.entity) return 'Оберіть сутність БД.'
  if (!draft.key.trim()) return 'Вкажіть ключ датасурсу.'
  if (draft.conditions.length === 0) return 'Додайте хоча б одну умову пошуку.'

  const invalidConditionIndex = draft.conditions.findIndex(
    (condition) =>
      !condition.property.trim() ||
      !condition.operator ||
      !condition.type ||
      !condition.value.trim(),
  )

  if (invalidConditionIndex !== -1) {
    return `Заповніть усі поля в умові пошуку ${invalidConditionIndex + 1}.`
  }

  return null
}

function coerceStep(step: number): ConstructorStep {
  return Math.min(4, Math.max(1, step)) as ConstructorStep
}

export const useConstructorStore = create<StoreState>()(
persist(
  (set, get) => ({
  currentStep: 1,
  mappingMode: 'scalars',
  selectedTag: null,
  selectedTable: null,
  newTableName: '',
  newColumnTag: '',
  newColumnPath: '',
  searchQuery: '',
  expandedSources: {},
  tagTypes: {},
  newSource: {
    entity: '',
    key: '',
    conditions: [{ ...defaultCondition, value: '' }],
  },
  config: initialConfig,
  constructorSessionKey: null,
  initialize: ({ tags, config, defaultEntity, sessionKey }) =>
    set((state) => {
      if (state.constructorSessionKey === sessionKey) {
        const nextTags = { ...state.tagTypes }
        tags.forEach((tag) => {
          nextTags[tag] ??= isReservedNumberTag(tag) ? 'reserved' : 'scalar'
        })
        return { tagTypes: nextTags }
      }

      return {
        currentStep: 1,
        mappingMode: 'scalars',
        selectedTag: null,
        selectedTable: null,
        newTableName: '',
        newColumnTag: '',
        newColumnPath: '',
        searchQuery: '',
        expandedSources: Object.fromEntries((config?.DataSources ?? []).map((source) => [source.Key, true])),
        tagTypes: Object.fromEntries(
          tags.map((tag) => [
            tag,
            isReservedNumberTag(tag)
              ? 'reserved'
              : config
                ? (config.Mapping.Scalars[tag] ? 'scalar' : 'table_column')
                : 'scalar',
          ]),
        ),
        config: config ? normalizeConfiguration(config) : initialConfig,
        constructorSessionKey: sessionKey,
        newSource: {
          entity: defaultEntity ?? '',
          key: '',
          conditions: [{ property: 'Id', operator: '==', type: 'arg', value: '' }],
        },
      }
    }),
  hydrateTags: (tags) =>
    set((state) => {
      const nextTags = { ...state.tagTypes }
      tags.forEach((tag) => {
        nextTags[tag] ??= isReservedNumberTag(tag) ? 'reserved' : 'scalar'
      })
      return { tagTypes: nextTags }
    }),
  setStep: (step) => set({ currentStep: step }),
  nextStep: () => {
    const { currentStep } = get()
    set({ currentStep: coerceStep(currentStep + 1) })
  },
  previousStep: () => set((state) => ({ currentStep: coerceStep(state.currentStep - 1) })),
  setMappingMode: (mappingMode) => set({ mappingMode }),
  setSelectedTag: (selectedTag) => set({ selectedTag }),
  setSelectedTable: (selectedTable) =>
    set((state) => ({
      selectedTable,
      newTableName: selectedTable ?? state.newTableName,
    })),
  setNewTableName: (newTableName) => set({ newTableName }),
  setNewColumnTag: (newColumnTag) => set({ newColumnTag }),
  setNewColumnPath: (newColumnPath) => set({ newColumnPath }),
  setSearchQuery: (searchQuery) => set({ searchQuery }),
  setTagType: (tag, type) =>
    set((state) => ({
      tagTypes: { ...state.tagTypes, [tag]: state.tagTypes[tag] === 'reserved' ? 'reserved' : type },
    })),
  updateNewSource: (patch) =>
    set((state) => ({
      newSource: { ...state.newSource, ...patch },
    })),
  updateNewSourceCondition: (index, patch) =>
    set((state) => ({
      newSource: {
        ...state.newSource,
        conditions: state.newSource.conditions.map((condition, conditionIndex) =>
          conditionIndex === index ? { ...condition, ...patch } : condition,
        ),
      },
    })),
  addNewSourceCondition: () =>
    set((state) => ({
      newSource: {
        ...state.newSource,
        conditions: [
          ...state.newSource.conditions,
          { property: 'Id', operator: '==', type: 'arg', value: '' },
        ],
      },
    })),
  removeNewSourceCondition: (index) =>
    set((state) => ({
      newSource: {
        ...state.newSource,
        conditions: state.newSource.conditions.filter((_, conditionIndex) => conditionIndex !== index),
      },
    })),
  validateNewDataSource: () => {
    const reason = validateNewDataSourceDraft(get().newSource)
    return reason ? { ok: false, reason } : { ok: true }
  },
  addDataSource: () => {
    const { newSource, config } = get()
    const key = newSource.key.trim()

    if (!key || !newSource.entity) return { ok: false, reason: 'Вкажіть сутність і ключ датасурсу.' }
    if (config.DataSources.some((source) => source.Key === key)) {
      return { ok: false, reason: 'Датасурс із таким ключем уже існує.' }
    }

    const dataSource = buildDataSource(newSource)
    set((state) => ({
      config: {
        ...state.config,
        DataSources: [...state.config.DataSources, dataSource],
      },
      expandedSources: { ...state.expandedSources, [dataSource.Key]: true },
      newSource: {
        ...state.newSource,
        key: '',
        conditions: [{ property: 'Id', operator: '==', type: 'arg', value: '' }],
      },
    }))

    return { ok: true }
  },
  removeDataSource: (key) =>
    set((state) => ({
      config: {
        ...state.config,
        DataSources: state.config.DataSources.filter((source) => source.Key !== key),
      },
    })),
  toggleExpanded: (key) =>
    set((state) => ({
      expandedSources: { ...state.expandedSources, [key]: !state.expandedSources[key] },
    })),
  mapScalar: (tag, fullPath) =>
    set((state) => ({
      config: {
        ...state.config,
        Mapping: {
          ...state.config.Mapping,
          Scalars: { ...state.config.Mapping.Scalars, [tag]: fullPath },
        },
      },
    })),
  unmapScalar: (tag) =>
    set((state) => {
      const restScalars = { ...state.config.Mapping.Scalars }
      delete restScalars[tag]
      return {
        selectedTag: state.selectedTag === tag ? null : state.selectedTag,
        config: {
          ...state.config,
          Mapping: { ...state.config.Mapping, Scalars: restScalars },
        },
      }
    }),
  createNewTable: () => {
    const { config, tagTypes } = get()
    const usedRowTags = new Set(
      Object.values(config.Mapping.Tables).flatMap((table) => Object.keys(table.RowMapping)),
    )
    const nextPrefixedTag = Object.entries(tagTypes).find(
      ([tag, type]) =>
        type === 'table_column' &&
        Boolean(getTableTagPrefix(tag)) &&
        !usedRowTags.has(getTableRowTagName(tag)),
    )?.[0]
    const tableName = getUniqueTableName(
      getTableTagPrefix(nextPrefixedTag ?? '') ?? `Table_${Math.floor(Math.random() * 1000)}`,
      config.Mapping.Tables,
    )

    set((state) => ({
      selectedTable: tableName,
      newTableName: tableName,
      config: {
        ...state.config,
        Mapping: {
          ...state.config.Mapping,
          Tables: {
            ...state.config.Mapping.Tables,
            [tableName]: { SourceArray: '', RowMapping: {} },
          },
        },
      },
    }))
  },
  deleteTable: (tableName) =>
    set((state) => {
      const tables = { ...state.config.Mapping.Tables }
      delete tables[tableName]
      return {
        selectedTable: state.selectedTable === tableName ? null : state.selectedTable,
        config: {
          ...state.config,
          Mapping: { ...state.config.Mapping, Tables: tables },
        },
      }
    }),
  renameTable: (oldName, newName) =>
    set((state) => {
      const trimmedName = newName.trim()
      if (!trimmedName || oldName === trimmedName || state.config.Mapping.Tables[trimmedName]) return state

      const { [oldName]: table, ...tables } = state.config.Mapping.Tables
      if (!table) return state

      return {
        selectedTable: trimmedName,
        newTableName: trimmedName,
        config: {
          ...state.config,
          Mapping: {
            ...state.config.Mapping,
            Tables: { ...tables, [trimmedName]: table },
          },
        },
      }
    }),
  updateTableSourceArray: (tableName, sourceArray) =>
    set((state) => {
      const table = state.config.Mapping.Tables[tableName]
      if (!table) return state

      return {
        config: {
          ...state.config,
          Mapping: {
            ...state.config.Mapping,
            Tables: {
              ...state.config.Mapping.Tables,
              [tableName]: { ...table, SourceArray: sourceArray },
            },
          },
        },
      }
    }),
  addColumnToTable: () =>
    set((state) => {
      const { selectedTable, newColumnTag, newColumnPath } = state
      if (!selectedTable || !newColumnTag || !newColumnPath) return state

      const table = state.config.Mapping.Tables[selectedTable]
      if (!table) return state

      return {
        newColumnTag: '',
        newColumnPath: '',
        config: {
          ...state.config,
          Mapping: {
            ...state.config.Mapping,
            Tables: {
              ...state.config.Mapping.Tables,
              [selectedTable]: {
                ...table,
                RowMapping: {
                  ...table.RowMapping,
                  [getTableRowTagName(newColumnTag)]: newColumnPath,
                },
              },
            },
          },
        },
      }
    }),
  removeColumnFromTable: (tableName, tag) =>
    set((state) => {
      const table = state.config.Mapping.Tables[tableName]
      if (!table) return state

      const rowMapping = { ...table.RowMapping }
      delete rowMapping[tag]
      return {
        config: {
          ...state.config,
          Mapping: {
            ...state.config.Mapping,
            Tables: {
              ...state.config.Mapping.Tables,
              [tableName]: { ...table, RowMapping: rowMapping },
            },
          },
        },
      }
    }),
  calculateIncludes: (schema = {}) =>
    set((state) => {
      const tables = Object.fromEntries(
        Object.entries(state.config.Mapping.Tables).map(([tableName, table]) => [
          tableName,
          { ...table, RowMapping: normalizeRowMapping(table.RowMapping) },
        ]),
      )
      const dataSources: DataSourceConfig[] = state.config.DataSources.map((source) => ({
        ...source,
        Includes: [],
      }))

      const addInclude = (dataSourceKey: string, include: string) => {
        const dataSource = dataSources.find((source) => source.Key === dataSourceKey)
        if (dataSource && !dataSource.Includes.includes(include)) {
          dataSource.Includes.push(include)
        }
      }

      const processPath = (fullPath: string) => {
        const parts = fullPath.split('.')
        if (parts.length < 3) return

        const [dataSourceKey, ...pathParts] = parts
        const dataSource = dataSources.find((source) => source.Key === dataSourceKey)
        if (!dataSource) return

        let currentEntity = dataSource.Entity
        const includeParts: string[] = []

        pathParts.slice(0, -1).forEach((part) => {
          const node = schema[currentEntity]
          if (!node) {
            includeParts.push(part)
            return
          }

          const nextEntity = node.entities[part] ?? node.collections[part]
          if (!nextEntity) return

          includeParts.push(part)
          currentEntity = nextEntity
        })

        if (includeParts.length > 0) {
          addInclude(dataSourceKey, includeParts.join('.'))
        }
      }

      Object.values(state.config.Mapping.Scalars).forEach(processPath)

      Object.values(tables).forEach((table) => {
        if (!table.SourceArray) return

        processPath(`${table.SourceArray}.FakeProp`)
        Object.values(table.RowMapping).forEach((columnPath) => {
          processPath(`${table.SourceArray}.${columnPath}`)
        })
      })

      return {
        config: {
          ...state.config,
          Mapping: {
            ...state.config.Mapping,
            Tables: tables,
          },
          DataSources: dataSources.map((source) => ({
            ...source,
            Includes: compactIncludes(source.Includes),
          })),
        },
      }
    }),
  reset: () =>
    set({
      currentStep: 1,
      mappingMode: 'scalars',
      selectedTag: null,
      selectedTable: null,
      newTableName: '',
      newColumnTag: '',
      newColumnPath: '',
      searchQuery: '',
      expandedSources: {},
      tagTypes: {},
      config: initialConfig,
      constructorSessionKey: null,
      newSource: {
        entity: '',
        key: '',
        conditions: [{ ...defaultCondition, value: '' }],
      },
    }),
  }),
  {
    name: 'template-constructor-state',
    storage: createJSONStorage(() => sessionStorage),
    partialize: (state) => ({
      currentStep: state.currentStep,
      mappingMode: state.mappingMode,
      selectedTag: state.selectedTag,
      selectedTable: state.selectedTable,
      newTableName: state.newTableName,
      newColumnTag: state.newColumnTag,
      newColumnPath: state.newColumnPath,
      searchQuery: state.searchQuery,
      expandedSources: state.expandedSources,
      tagTypes: state.tagTypes,
      newSource: state.newSource,
      config: state.config,
      constructorSessionKey: state.constructorSessionKey,
    }),
  },
))
