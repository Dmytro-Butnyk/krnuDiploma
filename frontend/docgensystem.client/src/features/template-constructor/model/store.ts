import { create } from 'zustand'
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

function isReservedNumberTag(tag: string) {
  return tag === 'Number' || tag.endsWith('.Number')
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
  initialize: (payload: {
    tags: string[]
    config?: TemplateConfiguration
    defaultEntity?: string
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

function coerceStep(step: number): ConstructorStep {
  return Math.min(4, Math.max(1, step)) as ConstructorStep
}

export const useConstructorStore = create<StoreState>((set, get) => ({
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
  initialize: ({ tags, config, defaultEntity }) =>
    set({
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
      config: config ?? initialConfig,
      newSource: {
        entity: defaultEntity ?? '',
        key: '',
        conditions: [{ property: 'Id', operator: '==', type: 'arg', value: '' }],
      },
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
    const tableName = `Table_${Math.floor(Math.random() * 1000)}`
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
                RowMapping: { ...table.RowMapping, [newColumnTag]: newColumnPath },
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
        let currentPath = ''

        pathParts.slice(0, -1).forEach((part) => {
          const node = schema[currentEntity]
          if (!node) return

          const nextEntity = node.entities[part] ?? node.collections[part]
          if (!nextEntity) return

          currentPath = currentPath ? `${currentPath}.${part}` : part
          addInclude(dataSourceKey, currentPath)
          currentEntity = nextEntity
        })
      }

      Object.values(state.config.Mapping.Scalars).forEach(processPath)

      Object.values(state.config.Mapping.Tables).forEach((table) => {
        if (!table.SourceArray) return

        processPath(`${table.SourceArray}.FakeProp`)
        Object.values(table.RowMapping).forEach((columnPath) => {
          processPath(`${table.SourceArray}.${columnPath}`)
        })
      })

      return {
        config: {
          ...state.config,
          DataSources: dataSources,
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
      config: initialConfig,
      newSource: {
        entity: '',
        key: '',
        conditions: [{ ...defaultCondition, value: '' }],
      },
    }),
}))
